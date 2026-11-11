using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.Models;
using ssw2026TermProject.Services;
using ssw2026TermProject.ViewModels;
using System.Security.Claims;

namespace ssw2026TermProject.Controllers
{
    // 【E2 新增】Save Listing 用例（页面 1~4）+ 网站公开的房源页
    // 流程：搜索客户 -> 选客户 -> 新建或修改房源 -> 保存确认
    [Authorize]
    public class ListingsController : Controller
    {
        // 规则 006：每个房源最多显示 7 张已批准图片
        private const int MaxDisplayImages = 7;

        private readonly ApplicationDbContext _context;
        private readonly CustomerSearchService _customerSearch;
        private readonly ListingService _listingService;
        private readonly IEmailService _emailService;

        public ListingsController(
            ApplicationDbContext context,
            CustomerSearchService customerSearch,
            ListingService listingService,
            IEmailService emailService)
        {
            _context = context;
            _customerSearch = customerSearch;
            _listingService = listingService;
            _emailService = emailService;
        }

        // =====================================================
        // 页面 1：客户搜索 + 结果列表
        // =====================================================

        // GET: Listings/SearchCustomer
        [HttpGet]
        public async Task<IActionResult> SearchCustomer([FromQuery] CustomerSearchViewModel model)
        {
            // 告诉共用的搜索组件：表单提交到哪里、选中后跳到哪里
            model.FormController = "Listings";
            model.FormAction = "SearchCustomer";
            model.SelectController = "Listings";
            model.SelectAction = "CustomerListings";

            // 刚打开页面（没点搜索）时只显示空表单
            if (model.Searched)
            {
                await _customerSearch.SearchAsync(model, ModelState);
            }

            return View(model);
        }

        // =====================================================
        // 页面 2：客户房源概览
        // =====================================================

        // GET: Listings/CustomerListings?customerId=5
        [HttpGet]
        public async Task<IActionResult> CustomerListings(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == customerId);
            if (customer == null)
            {
                // 客户不存在：提示先去 Create Customer
                TempData["Error"] = "That customer does not exist. Please create the customer first.";
                return RedirectToAction(nameof(SearchCustomer));
            }

            // 先把到期的房源状态更新掉，再读取
            await _listingService.ExpireOverdueListingsAsync();

            var listings = await _context.Listings
                .Include(l => l.Agent)
                .Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.ListingId)
                .ToListAsync();

            return View(new CustomerListingsViewModel { Customer = customer, Listings = listings });
        }

        // =====================================================
        // 页面 3：房源表单（新建和修改共用同一个视图 Form.cshtml）
        // =====================================================

        // GET: Listings/Create?customerId=5
        [HttpGet]
        public async Task<IActionResult> Create(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == customerId);
            if (customer == null)
            {
                TempData["Error"] = "That customer does not exist. Please create the customer first.";
                return RedirectToAction(nameof(SearchCustomer));
            }

            var model = new ListingFormViewModel
            {
                Listing = new Listing { CustomerId = customerId },
                IsEdit = false
            };
            await PopulateFormAsync(model, customer);
            return View("Form", model);
        }

        // POST: Listings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingFormViewModel model)
        {
            return await SaveAsync(model, existing: null);
        }

        // GET: Listings/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await _listingService.ExpireOverdueListingsAsync();

            var listing = await _context.Listings.FirstOrDefaultAsync(l => l.ListingId == id);
            if (listing == null)
            {
                return NotFound();
            }

            // 已取消的房源不能再修改
            if (listing.Status == ListingStatus.Cancelled)
            {
                TempData["Error"] = "A cancelled listing cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == listing.CustomerId);

            var model = new ListingFormViewModel { Listing = listing, IsEdit = true };
            await PopulateFormAsync(model, customer);

            // 把已经选中显示的图片预先勾上
            model.SelectedImageIds = model.ApprovedImages
                .Where(i => i.DisplayOnListing)
                .Select(i => i.UploadedImageId)
                .ToList();

            return View("Form", model);
        }

        // POST: Listings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ListingFormViewModel model)
        {
            if (id != model.Listing.ListingId)
            {
                return NotFound();
            }

            await _listingService.ExpireOverdueListingsAsync();

            var existing = await _context.Listings.FirstOrDefaultAsync(l => l.ListingId == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (existing.Status == ListingStatus.Cancelled)
            {
                TempData["Error"] = "A cancelled listing cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return await SaveAsync(model, existing);
        }

        // 新建和修改共用的保存逻辑
        // existing 为 null 表示新建；否则是数据库里已有的房源（被跟踪的实体）
        private async Task<IActionResult> SaveAsync(ListingFormViewModel model, Listing? existing)
        {
            bool isEdit = existing != null;
            model.IsEdit = isEdit;

            // 客户以数据库为准：修改时不允许通过表单偷偷换客户
            int customerId = isEdit ? existing!.CustomerId : model.Listing.CustomerId;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == customerId);
            if (customer == null)
            {
                TempData["Error"] = "That customer does not exist. Please create the customer first.";
                return RedirectToAction(nameof(SearchCustomer));
            }
            model.Listing.CustomerId = customerId;

            // 规则 003：合同期内（已签且 Available）不能更换经纪人，也不能把合同改回"未签"
            bool contractActive = isEdit && ListingService.IsContractActive(existing!);
            model.AgentLocked = contractActive;

            int? postedAgentId = model.Listing.AgentId;

            // ---------- 合同 / 经纪人规则校验 ----------
            if (model.Listing.ContractSigned)
            {
                // 规则 002：合同已签就必须有经纪人
                if (postedAgentId == null)
                {
                    ModelState.AddModelError("Listing.AgentId", "An agent must be selected when the contract is signed.");
                }
                else if (!await _context.Agents.AnyAsync(a => a.AgentId == postedAgentId))
                {
                    ModelState.AddModelError("Listing.AgentId", "The selected agent does not exist.");
                }

                // 规则 003：合同期内不能换经纪人
                if (contractActive && postedAgentId != existing!.AgentId)
                {
                    ModelState.AddModelError("Listing.AgentId", "The agent cannot be changed while the contract is active.");
                }
            }
            else if (contractActive)
            {
                ModelState.AddModelError("Listing.ContractSigned", "The contract is active and cannot be marked as unsigned.");
            }

            // ---------- 图片选择校验（规则 006） ----------
            var selectedIds = model.SelectedImageIds.Distinct().ToList();
            if (selectedIds.Count > MaxDisplayImages)
            {
                ModelState.AddModelError(string.Empty, $"You can select at most {MaxDisplayImages} pictures.");
            }
            else if (selectedIds.Count > 0)
            {
                // 只允许选"属于该房源且已批准"的图片
                int validCount = isEdit
                    ? await _context.UploadedImages.CountAsync(i =>
                        selectedIds.Contains(i.UploadedImageId) && i.ListingId == existing!.ListingId && i.IsApproved)
                    : 0;
                if (validCount != selectedIds.Count)
                {
                    ModelState.AddModelError(string.Empty, "Only approved pictures of this listing can be selected.");
                }
            }

            if (!ModelState.IsValid)
            {
                // 修改模式下，起止日期和状态只是只读展示（不随表单提交），回显数据库里的值
                if (isEdit)
                {
                    model.Listing.StartDate = existing!.StartDate;
                    model.Listing.EndDate = existing.EndDate;
                    model.Listing.Status = existing.Status;
                }

                // 校验失败：重新准备下拉框等数据，回到表单让用户重试
                await PopulateFormAsync(model, customer);
                return View("Form", model);
            }

            // ---------- 通过校验，写入数据库 ----------
            Listing target;
            if (isEdit)
            {
                target = existing!;
            }
            else
            {
                target = new Listing
                {
                    CustomerId = customerId,
                    CreatedDate = DateTime.Now,
                    CreatedByStaffId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };
                _context.Listings.Add(target);
            }

            // 只复制允许修改的字段，避免被恶意提交覆盖其他字段（防 overposting）
            CopyEditableFields(model.Listing, target);

            bool newlySigned = false;
            if (model.Listing.ContractSigned)
            {
                if (!contractActive)
                {
                    // 新签约，或到期后续约：绑定经纪人，起始日期为今天，结束日期为 3 个月后，状态变为 Available
                    target.ContractSigned = true;
                    target.AgentId = postedAgentId;
                    target.StartDate = DateTime.Today;
                    target.EndDate = DateTime.Today.AddMonths(3);
                    target.Status = ListingStatus.Available;
                    newlySigned = true;
                }
                // 合同期内再次保存：经纪人、日期、状态都保持不变
            }
            else
            {
                // 规则 007：合同未签可以保存，但不关联经纪人、状态 Not Available、没有起止日期、不上网站
                target.ContractSigned = false;
                target.AgentId = null;
                target.StartDate = null;
                target.EndDate = null;
                target.Status = ListingStatus.NotAvailable;
            }

            await _context.SaveChangesAsync();

            // 更新图片的"显示在房源页"标记（只有修改模式才有图片可选）
            if (isEdit)
            {
                var images = await _context.UploadedImages
                    .Where(i => i.ListingId == target.ListingId)
                    .ToListAsync();
                foreach (var image in images)
                {
                    image.DisplayOnListing = image.IsApproved && selectedIds.Contains(image.UploadedImageId);
                }
                await _context.SaveChangesAsync();
            }

            // 合同刚签署（或续约）：给客户发邮件，内容包含房源编号和线上链接
            if (newlySigned)
            {
                string link = Url.Action(nameof(Property), "Listings", new { id = target.ListingId }, Request.Scheme)
                              ?? string.Empty;
                await _emailService.SendListingSignedEmailAsync(customer, target, link);
            }

            // 保存成功的确认消息（页面 4 顶部显示）
            string successMessage = isEdit
                ? $"Listing #{target.ListingId} was updated successfully."
                : $"Listing #{target.ListingId} was saved successfully.";
            if (newlySigned)
            {
                successMessage += " A confirmation email was sent to the customer.";
            }
            TempData["Success"] = successMessage;

            return RedirectToAction(nameof(Details), new { id = target.ListingId });
        }

        // 把表单里允许编辑的字段复制到实体上
        private static void CopyEditableFields(Listing source, Listing target)
        {
            target.StreetAddress = source.StreetAddress?.Trim();
            target.City = source.City?.Trim();
            target.Province = source.Province?.Trim();
            target.PostalCode = source.PostalCode?.Trim();
            target.SquareFeet = source.SquareFeet;
            target.Bedrooms = source.Bedrooms;
            target.Bathrooms = source.Bathrooms;
            target.Floors = source.Floors;
            target.Area = source.Area;
            target.FeatureDescription = source.FeatureDescription?.Trim();
            target.HeatingType = source.HeatingType;
            target.HasFireplace = source.HasFireplace;
            target.GarageSpaces = source.GarageSpaces;
            target.HasBabyBarn = source.HasBabyBarn;
            target.HasCentralAir = source.HasCentralAir;
            target.HasScreens = source.HasScreens;
            target.AskingPrice = source.AskingPrice;
        }

        // 准备表单需要的显示数据：客户、经纪人列表、已批准图片
        private async Task PopulateFormAsync(ListingFormViewModel model, Customer? customer)
        {
            model.Customer = customer;

            model.Agents = await _context.Agents
                .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
                .ToListAsync();

            // 修改模式：读取该房源的图片，已批准的可选，未批准的只统计数量
            if (model.Listing.ListingId > 0)
            {
                var images = await _context.UploadedImages
                    .Where(i => i.ListingId == model.Listing.ListingId)
                    .OrderBy(i => i.UploadedImageId)
                    .ToListAsync();

                model.ApprovedImages = images.Where(i => i.IsApproved).ToList();
                model.PendingImageCount = images.Count(i => !i.IsApproved);

                // 合同期内锁定经纪人下拉框
                var stored = await _context.Listings.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.ListingId == model.Listing.ListingId);
                model.AgentLocked = stored != null && ListingService.IsContractActive(stored);
            }
        }

        // =====================================================
        // 页面 4：房源详情 / 取消页
        // =====================================================

        // GET: Listings/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            await _listingService.ExpireOverdueListingsAsync();

            var listing = await _context.Listings
                .Include(l => l.Customer)
                .Include(l => l.Agent)
                .FirstOrDefaultAsync(l => l.ListingId == id);
            if (listing == null)
            {
                return NotFound();
            }

            var images = await _context.UploadedImages
                .Where(i => i.ListingId == id)
                .ToListAsync();

            var email = await _context.SimulatedEmails
                .Where(e => e.ListingId == id)
                .OrderByDescending(e => e.SimulatedEmailId)
                .FirstOrDefaultAsync();

            var model = new ListingDetailsViewModel
            {
                Listing = listing,
                DisplayImages = images
                    .Where(i => i.IsApproved && i.DisplayOnListing)
                    .Take(MaxDisplayImages)
                    .ToList(),
                PendingImageCount = images.Count(i => !i.IsApproved),
                LatestEmail = email
            };

            // 规则 008：未分配经纪人时普通员工可取消；已分配经纪人时只有 Broker 可取消
            if (listing.Status != ListingStatus.Cancelled)
            {
                bool allowed = CanCancelListing(listing);
                model.CanCancel = allowed;
                model.CancelBlockedByRole = !allowed;
            }

            return View(model);
        }

        // POST: Listings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var listing = await _context.Listings.FirstOrDefaultAsync(l => l.ListingId == id);
            if (listing == null)
            {
                return NotFound();
            }

            if (listing.Status == ListingStatus.Cancelled)
            {
                TempData["Error"] = "This listing has already been cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // 即使有人绕过页面按钮直接提交，这里也会再检查一次权限
            if (!CanCancelListing(listing))
            {
                TempData["Error"] = "You do not have permission to cancel this listing. Only a Broker can cancel a listing that has an agent assigned.";
                return RedirectToAction(nameof(Details), new { id });
            }

            listing.Status = ListingStatus.Cancelled;
            listing.CancelledDate = DateTime.Now;
            listing.CancelledByStaffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Listing #{listing.ListingId} was cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 规则 008 的权限判断
        private bool CanCancelListing(Listing listing)
        {
            if (listing.AgentId == null)
            {
                return true; // 未分配经纪人：任何已登录员工都可以取消
            }
            return User.IsInRole(AppRoles.Broker); // 已分配经纪人：只有 Broker
        }

        // =====================================================
        // 网站公开页（不需要登录）：只显示合同已签且状态 Available 的房源
        // =====================================================

        // GET: Listings/Browse?q=xxx
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Browse(string? q)
        {
            await _listingService.ExpireOverdueListingsAsync();

            var query = _context.Listings
                .Include(l => l.Agent)
                .Where(l => l.ContractSigned && l.Status == ListingStatus.Available);

            string? keyword = q?.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(l =>
                    (l.StreetAddress != null && l.StreetAddress.Contains(keyword)) ||
                    (l.City != null && l.City.Contains(keyword)) ||
                    (l.FeatureDescription != null && l.FeatureDescription.Contains(keyword)));
            }

            var listings = await query.OrderByDescending(l => l.ListingId).ToListAsync();
            var ids = listings.Select(l => l.ListingId).ToList();

            // 每个房源取一张封面：已批准且被选中显示的图片；没有就在页面上显示占位提示
            var images = await _context.UploadedImages
                .Where(i => i.ListingId != null && ids.Contains(i.ListingId.Value)
                            && i.IsApproved && i.DisplayOnListing)
                .OrderBy(i => i.UploadedImageId)
                .ToListAsync();

            var covers = new Dictionary<int, UploadedImage>();
            foreach (var image in images)
            {
                covers.TryAdd(image.ListingId!.Value, image);
            }

            return View(new BrowseViewModel { Query = keyword, Listings = listings, CoverImages = covers });
        }

        // GET: Listings/Property/5
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Property(int id)
        {
            await _listingService.ExpireOverdueListingsAsync();

            var listing = await _context.Listings
                .Include(l => l.Agent)
                .FirstOrDefaultAsync(l => l.ListingId == id
                                          && l.ContractSigned
                                          && l.Status == ListingStatus.Available);
            if (listing == null)
            {
                // 未签合同、已过期、已取消的房源在网站上不可见
                return NotFound();
            }

            var images = await _context.UploadedImages
                .Where(i => i.ListingId == id && i.IsApproved && i.DisplayOnListing)
                .OrderBy(i => i.UploadedImageId)
                .Take(MaxDisplayImages)
                .ToListAsync();

            return View(new PublicPropertyViewModel { Listing = listing, Images = images });
        }
    }
}
