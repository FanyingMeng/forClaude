using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.Models;
using ssw2026TermProject.ViewModels;
using System.Security.Claims;

namespace ssw2026TermProject.Controllers
{
    // 【E2 改版】页面 7：上传图片（比 E1 多了"选类型"和"搜索并关联对象"两步）
    // 第一步 Index：选图片类型（经纪人照片 / 房源照片）
    // 第二步 SelectTarget：房源按客户名搜索，经纪人按经纪人名搜索，并选中一条记录
    // 第三步 Details：浏览文件、自定义文件名、填写描述和替代文字，校验后上传
    // 规则 017：所有员工都可以上传（审批人必须是另一位员工，审核功能本轮只预留字段）
    [Authorize]
    public class UploadController : Controller
    {
        // 允许的图片扩展名
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // 文件大小上限 5 MB（沿用 E1 的限制）
        private const long MaxFileSize = 5 * 1024 * 1024;

        // 文件名（不含扩展名）最长字符数
        private const int MaxFileNameLength = 100;

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UploadController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =====================================================
        // 第一步：选择图片类型
        // =====================================================

        // GET: Upload
        [HttpGet]
        public IActionResult Index()
        {
            return View(new UploadTypeViewModel());
        }

        // POST: Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(UploadTypeViewModel model)
        {
            // 没选类型不允许继续，并提示
            if (model.ImageType == null || !Enum.IsDefined(model.ImageType.Value))
            {
                ModelState.AddModelError(nameof(model.ImageType), "Please select an image type.");
                return View(model);
            }

            return RedirectToAction(nameof(SelectTarget), new { imageType = model.ImageType.Value });
        }

        // =====================================================
        // 第二步：搜索并选择关联对象
        // =====================================================

        // GET: Upload/SelectTarget?imageType=2&searched=true&name=smith
        [HttpGet]
        public async Task<IActionResult> SelectTarget([FromQuery] UploadTargetViewModel model)
        {
            if (!Enum.IsDefined(model.ImageType))
            {
                return RedirectToAction(nameof(Index));
            }

            if (model.Searched)
            {
                await SearchTargetsAsync(model);
            }

            return View(model);
        }

        // POST: Upload/SelectTarget  （点击"Continue"，必须选中一条记录才能继续）
        // C# 方法名用 SelectTargetPost（参数类型与 GET 版本相同，不能重名），对外的动作名仍然是 SelectTarget
        [HttpPost]
        [ActionName("SelectTarget")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectTargetPost(UploadTargetViewModel model)
        {
            if (!Enum.IsDefined(model.ImageType))
            {
                return RedirectToAction(nameof(Index));
            }

            if (model.SelectedId == null)
            {
                // 没选中任何记录：重新显示结果并提示
                ModelState.AddModelError(nameof(model.SelectedId), "Please select one record to continue.");
                model.Searched = true;
                await SearchTargetsAsync(model);
                return View(model);
            }

            // 再确认一次选中的对象确实存在
            if (await ResolveTargetAsync(model.ImageType, model.SelectedId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.SelectedId), "The selected record could not be found. Please search again.");
                model.Searched = true;
                await SearchTargetsAsync(model);
                return View(model);
            }

            return RedirectToAction(nameof(Details), new
            {
                imageType = model.ImageType,
                targetId = model.SelectedId.Value
            });
        }

        // 执行搜索：房源 -> 按客户名搜；经纪人 -> 按经纪人名搜
        private async Task SearchTargetsAsync(UploadTargetViewModel model)
        {
            string[] tokens = (model.Name ?? string.Empty)
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                ModelState.AddModelError(nameof(model.Name), "Please enter a name to search for.");
                return;
            }

            if (model.ImageType == UploadImageType.ListingPhoto)
            {
                // 先按客户名找客户（每个词都要在名/中间名/姓里出现）
                var customerQuery = _context.Customers.AsQueryable();
                foreach (string token in tokens)
                {
                    string term = token;
                    customerQuery = customerQuery.Where(c =>
                        (c.FirstName != null && c.FirstName.Contains(term)) ||
                        (c.MiddleName != null && c.MiddleName.Contains(term)) ||
                        (c.LastName != null && c.LastName.Contains(term)));
                }
                var customerIds = await customerQuery.Select(c => c.ID ?? 0).ToListAsync();

                // 再找这些客户的房源（已取消的房源不再接受上传）
                model.Listings = await _context.Listings
                    .Include(l => l.Customer)
                    .Where(l => customerIds.Contains(l.CustomerId) && l.Status != ListingStatus.Cancelled)
                    .OrderBy(l => l.CustomerId).ThenBy(l => l.ListingId)
                    .ToListAsync();
            }
            else
            {
                var agentQuery = _context.Agents.AsQueryable();
                foreach (string token in tokens)
                {
                    string term = token;
                    agentQuery = agentQuery.Where(a =>
                        (a.FirstName != null && a.FirstName.Contains(term)) ||
                        (a.MiddleName != null && a.MiddleName.Contains(term)) ||
                        (a.LastName != null && a.LastName.Contains(term)));
                }
                model.Agents = await agentQuery
                    .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
                    .ToListAsync();
            }

            // 找不到对象：提示员工重试
            if (model.Listings.Count == 0 && model.Agents.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Name),
                    model.ImageType == UploadImageType.ListingPhoto
                        ? "No listings were found for that customer name. Please try again."
                        : "No agents were found with that name. Please try again.");
            }
        }

        // =====================================================
        // 第三步：选文件、填写元数据并上传
        // =====================================================

        // GET: Upload/Details?imageType=2&targetId=7
        [HttpGet]
        public async Task<IActionResult> Details(UploadImageType imageType, int targetId)
        {
            var target = await ResolveTargetAsync(imageType, targetId);
            if (target == null)
            {
                TempData["Error"] = "The selected record could not be found. Please search again.";
                return RedirectToAction(nameof(SelectTarget), new { imageType });
            }

            return View(new UploadFileViewModel
            {
                ImageType = imageType,
                TargetId = targetId,
                TargetLabel = target.Value.Label,
                // 文件名规则（规则 013）作为默认值填进输入框；员工可以改，清空则使用原文件名
                CustomFileName = target.Value.RuleName
            });
        }

        // POST: Upload/Details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(UploadFileViewModel model, IFormFile? imageFile)
        {
            var target = await ResolveTargetAsync(model.ImageType, model.TargetId);
            if (target == null)
            {
                TempData["Error"] = "The selected record could not be found. Please search again.";
                return RedirectToAction(nameof(SelectTarget), new { imageType = model.ImageType });
            }
            model.TargetLabel = target.Value.Label;

            // ---------- 1. 校验文件（任何一项失败：不上传、不保存元数据） ----------
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Please select an image.");
                return View(model);
            }

            string extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !await LooksLikeImageAsync(imageFile, extension))
            {
                ModelState.AddModelError("imageFile", "Invalid image format. Only JPG, PNG and WEBP images are allowed.");
                return View(model);
            }

            if (imageFile.Length > MaxFileSize)
            {
                ModelState.AddModelError("imageFile", "Image cannot exceed 5 MB.");
                return View(model);
            }

            string? staffMemberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(staffMemberId))
            {
                return Challenge();
            }

            // ---------- 2. 确定文件名：自定义名优先，留空则用原文件名 ----------
            string baseFileName = CleanFileName(model.CustomFileName, stripKnownExtension: true);
            if (string.IsNullOrEmpty(baseFileName))
            {
                baseFileName = CleanFileName(Path.GetFileNameWithoutExtension(imageFile.FileName), stripKnownExtension: false);
            }
            if (string.IsNullOrEmpty(baseFileName))
            {
                ModelState.AddModelError(nameof(model.CustomFileName), "Invalid file name.");
                return View(model);
            }

            string tempFolder = Path.Combine(_environment.WebRootPath, "tempImages");
            string approvedFolder = Path.Combine(_environment.WebRootPath, "approvedImages");
            Directory.CreateDirectory(tempFolder);
            Directory.CreateDirectory(approvedFolder);

            // 规则 013：重名时允许上传，在物理文件名后面加一个唯一的数字；元数据里也用这个新名字
            string fileName = baseFileName + extension;
            int counter = 1;
            while (System.IO.File.Exists(Path.Combine(tempFolder, fileName)) ||
                   System.IO.File.Exists(Path.Combine(approvedFolder, fileName)))
            {
                fileName = baseFileName + counter + extension;
                counter++;
            }

            // ---------- 3. 保存文件到 tempImages 文件夹 ----------
            string tempFilePath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(tempFilePath, FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            // ---------- 4. 保存元数据，并关联到所选房源或经纪人 ----------
            var uploadedImage = new UploadedImage
            {
                // 未审核图片放在 tempImages；审核通过后再挪到 approvedImages 并更新路径
                FilePath = "tempImages/" + fileName,
                FileName = fileName,
                Description = model.Description?.Trim(),
                AltText = model.AltText?.Trim(),
                UploadDateTime = DateTime.Now,
                StaffMemberId = staffMemberId,   // 上传员工 ID
                IsApproved = false,              // 规则 012：默认"未审核"
                ImageType = model.ImageType,
                AgentId = model.ImageType == UploadImageType.AgentPhoto ? model.TargetId : null,
                ListingId = model.ImageType == UploadImageType.ListingPhoto ? model.TargetId : null
            };

            _context.UploadedImages.Add(uploadedImage);
            await _context.SaveChangesAsync();

            // 上传成功后显示确认消息（PRG：重定向回第三步页面，避免刷新重复提交）
            TempData["Success"] = $"Image \"{fileName}\" was uploaded successfully and is waiting for approval. " +
                                  "It will show as \"Picture Not Available\" on the website until it is approved.";
            return RedirectToAction(nameof(Details), new { imageType = model.ImageType, targetId = model.TargetId });
        }

        // =====================================================
        // 辅助方法
        // =====================================================

        // 根据类型和 ID 找到关联对象，返回显示用的名称和"文件名规则"生成的默认文件名
        // 规则 013：经纪人 = "名 中间名 姓"；房源 = "房源编号 + 街道地址"
        private async Task<(string Label, string RuleName)?> ResolveTargetAsync(UploadImageType type, int targetId)
        {
            if (type == UploadImageType.AgentPhoto)
            {
                var agent = await _context.Agents.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AgentId == targetId);
                if (agent == null)
                {
                    return null;
                }

                string fullName = string.Join(" ", new[] { agent.FirstName, agent.MiddleName, agent.LastName }
                    .Where(p => !string.IsNullOrWhiteSpace(p)));
                return ($"Agent: {fullName}", fullName);
            }

            if (type == UploadImageType.ListingPhoto)
            {
                var listing = await _context.Listings.AsNoTracking()
                    .Include(l => l.Customer)
                    .FirstOrDefaultAsync(l => l.ListingId == targetId);
                if (listing == null || listing.Status == ListingStatus.Cancelled)
                {
                    return null;
                }

                string owner = $"{listing.Customer?.FirstName} {listing.Customer?.LastName}".Trim();
                return ($"Listing #{listing.ListingId} - {listing.StreetAddress}, {listing.City} (owner: {owner})",
                        $"{listing.ListingId} {listing.StreetAddress}");
            }

            return null;
        }

        // 清理文件名：去掉非法字符、多余空格，并限制长度
        private static string CleanFileName(string? name, bool stripKnownExtension)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string result = name.Trim();

            // 员工自己在自定义名里写了 .jpg 之类的扩展名时，去掉它（真正的扩展名以所选文件为准）
            if (stripKnownExtension && AllowedExtensions.Contains(Path.GetExtension(result).ToLowerInvariant()))
            {
                result = Path.GetFileNameWithoutExtension(result);
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c.ToString(), string.Empty);
            }

            // 去掉前后的点和空格（防止出现隐藏文件或以点结尾的名字），连续空格合并成一个
            result = string.Join(" ", result.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim('.', ' ');

            if (result.Length > MaxFileNameLength)
            {
                result = result.Substring(0, MaxFileNameLength).TrimEnd('.', ' ');
            }

            return result;
        }

        // 检查文件头（魔数），防止把非图片文件改个扩展名就传上来（规则 012：图片安全）
        private static async Task<bool> LooksLikeImageAsync(IFormFile file, string extension)
        {
            byte[] header = new byte[12];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAsync(header, 0, header.Length);
            }

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    // JPEG 以 FF D8 FF 开头
                    return read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

                case ".png":
                    // PNG 以 89 50 4E 47 0D 0A 1A 0A 开头
                    return read >= 8
                        && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                        && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

                case ".webp":
                    // WEBP：开头是 "RIFF"，第 8~11 字节是 "WEBP"
                    return read >= 12
                        && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                        && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

                default:
                    return false;
            }
        }
    }
}
