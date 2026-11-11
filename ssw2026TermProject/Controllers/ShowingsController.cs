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
    // 【E2 新增】Save Showing 用例（页面 5~6）
    // 预约 = 某客户在某日期、时间段，与某经纪人一起看某房源。保存时必须检查冲突。
    [Authorize]
    public class ShowingsController : Controller
    {
        // 规则 015：预约前后各要留 15 分钟路程时间
        private const int TravelBufferMinutes = 15;

        private readonly ApplicationDbContext _context;
        private readonly CustomerSearchService _customerSearch;
        private readonly ListingService _listingService;
        private readonly CurrentAgentService _currentAgentService;

        public ShowingsController(
            ApplicationDbContext context,
            CustomerSearchService customerSearch,
            ListingService listingService,
            CurrentAgentService currentAgentService)
        {
            _context = context;
            _customerSearch = customerSearch;
            _listingService = listingService;
            _currentAgentService = currentAgentService;
        }

        // =====================================================
        // 页面 5 的第一步：选客户（复用客户搜索部分视图）
        // =====================================================

        // GET: Showings/SelectCustomer
        // 规则 014：只有 Office Manager / Broker / Agent 能创建预约，前台访问会被拒绝
        [Authorize(Roles = AppRoles.CanBookShowings)]
        [HttpGet]
        public async Task<IActionResult> SelectCustomer([FromQuery] CustomerSearchViewModel model)
        {
            model.FormController = "Showings";
            model.FormAction = "SelectCustomer";
            model.SelectController = "Showings";
            model.SelectAction = "Create";

            if (model.Searched)
            {
                await _customerSearch.SearchAsync(model, ModelState);
            }

            return View(model);
        }

        // =====================================================
        // 页面 5：预约表单（新建和修改共用 Form.cshtml）
        // =====================================================

        // GET: Showings/Create?customerId=5
        [Authorize(Roles = AppRoles.CanBookShowings)]
        [HttpGet]
        public async Task<IActionResult> Create(int? customerId)
        {
            if (customerId == null)
            {
                return RedirectToAction(nameof(SelectCustomer));
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == customerId);
            if (customer == null)
            {
                // 客户不存在：提示先去 Create Customer
                TempData["Error"] = "That customer does not exist. Please create the customer first.";
                return RedirectToAction(nameof(SelectCustomer));
            }

            var model = new ShowingFormViewModel
            {
                Showing = new Showing { CustomerId = customer.ID ?? 0 },
                IsEdit = false
            };
            await PopulateFormAsync(model, customer);
            return View("Form", model);
        }

        // POST: Showings/Create
        [Authorize(Roles = AppRoles.CanBookShowings)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShowingFormViewModel model)
        {
            return await SaveAsync(model, existing: null);
        }

        // GET: Showings/Edit/5
        [Authorize(Roles = AppRoles.CanBookShowings)]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var showing = await _context.Showings.FirstOrDefaultAsync(s => s.ShowingId == id);
            if (showing == null)
            {
                return NotFound();
            }

            // 规则 014：Office Manager 或相关经纪人才能修改
            if (!await CanEditShowingAsync(showing))
            {
                return Forbid();
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == showing.CustomerId);

            var model = new ShowingFormViewModel { Showing = showing, IsEdit = true };
            await PopulateFormAsync(model, customer);
            return View("Form", model);
        }

        // POST: Showings/Edit/5
        [Authorize(Roles = AppRoles.CanBookShowings)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ShowingFormViewModel model)
        {
            if (id != model.Showing.ShowingId)
            {
                return NotFound();
            }

            var existing = await _context.Showings.FirstOrDefaultAsync(s => s.ShowingId == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!await CanEditShowingAsync(existing))
            {
                return Forbid();
            }

            return await SaveAsync(model, existing);
        }

        // 新建和修改共用的保存逻辑
        private async Task<IActionResult> SaveAsync(ShowingFormViewModel model, Showing? existing)
        {
            bool isEdit = existing != null;
            model.IsEdit = isEdit;

            bool isManager = User.IsInRole(AppRoles.OfficeManager);
            Agent? me = await _currentAgentService.GetAsync(User);

            // 客户以数据库为准：修改时不能换客户
            int customerId = isEdit ? existing!.CustomerId : model.Showing.CustomerId;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == customerId);
            if (customer == null)
            {
                TempData["Error"] = "That customer does not exist. Please create the customer first.";
                return RedirectToAction(nameof(SelectCustomer));
            }
            model.Showing.CustomerId = customerId;

            // ---------- 经纪人（规则 014 / 016） ----------
            int? agentId;
            if (isManager)
            {
                // Office Manager：从列表里选，没选就不能保存（Required 已经会报错，这里是兜底）
                agentId = model.Showing.AgentId;
            }
            else if (me == null)
            {
                // 普通经纪人账号没有对应的经纪人记录，无法确定"自己"是谁
                agentId = null;
                ModelState.AddModelError(string.Empty,
                    "Your login is not linked to an agent record, so a showing cannot be booked for you. Please ask the Office Manager.");
            }
            else
            {
                // 规则 016：经纪人自己预约时，强制使用他本人，忽略表单提交的值
                agentId = me.AgentId;
                ModelState.Remove("Showing.AgentId");
            }
            model.Showing.AgentId = agentId;

            if (agentId != null && !await _context.Agents.AnyAsync(a => a.AgentId == agentId))
            {
                ModelState.AddModelError("Showing.AgentId", "The selected agent does not exist.");
            }

            // ---------- 房源：必须存在且为 Available（修改时允许保留原来的房源） ----------
            int? listingId = model.Showing.ListingId;
            if (listingId != null)
            {
                var listing = await _context.Listings.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.ListingId == listingId);
                bool unchanged = isEdit && existing!.ListingId == listingId;

                if (listing == null)
                {
                    ModelState.AddModelError("Showing.ListingId", "The selected listing does not exist.");
                }
                else if (listing.Status != ListingStatus.Available && !unchanged)
                {
                    ModelState.AddModelError("Showing.ListingId", "Only available listings can be booked.");
                }
            }

            // ---------- 日期时间有效性 ----------
            DateOnly? date = model.Showing.ShowingDate;
            TimeOnly? start = model.Showing.StartTime;
            TimeOnly? end = model.Showing.EndTime;

            if (start != null && end != null && end <= start)
            {
                ModelState.AddModelError("Showing.EndTime", "End time must be later than the start time.");
            }

            if (date != null && start != null)
            {
                // 修改时如果日期和开始时间没变，允许保留（比如只改备注）
                bool timeChanged = !isEdit || existing!.ShowingDate != date || existing.StartTime != start;
                if (timeChanged)
                {
                    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                    if (date < today)
                    {
                        ModelState.AddModelError("Showing.ShowingDate", "The date cannot be in the past.");
                    }
                    else if (date == today && start < TimeOnly.FromDateTime(DateTime.Now))
                    {
                        ModelState.AddModelError("Showing.StartTime", "The start time has already passed.");
                    }
                }
            }

            // ---------- 冲突检查（所有基本校验通过后才查） ----------
            if (ModelState.IsValid && date != null && start != null && end != null
                && agentId != null && listingId != null)
            {
                string? conflict = await FindConflictAsync(
                    date.Value, start.Value, end.Value, agentId.Value, listingId.Value,
                    isEdit ? existing!.ShowingId : (int?)null);

                if (conflict != null)
                {
                    ModelState.AddModelError(string.Empty, conflict);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormAsync(model, customer);
                return View("Form", model);
            }

            // ---------- 保存 ----------
            Showing target;
            if (isEdit)
            {
                target = existing!;
            }
            else
            {
                target = new Showing
                {
                    CustomerId = customerId,
                    CreatedDate = DateTime.Now,
                    CreatedByStaffId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };
                _context.Showings.Add(target);
            }

            target.ListingId = listingId;
            target.AgentId = agentId;
            target.ShowingDate = date;
            target.StartTime = start;
            target.EndTime = end;
            target.Notes = model.Showing.Notes?.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = isEdit
                ? $"Showing #{target.ShowingId} was updated successfully."
                : $"Showing #{target.ShowingId} was booked successfully.";
            return RedirectToAction(nameof(Details), new { id = target.ShowingId });
        }

        // 冲突检查：返回冲突说明；没有冲突返回 null
        // 规则 015：经纪人的预约前后各要留 15 分钟路程时间；
        // 同一房源在同一时间不能被另一位经纪人带看
        private async Task<string?> FindConflictAsync(
            DateOnly date, TimeOnly start, TimeOnly end,
            int agentId, int listingId, int? ignoreShowingId)
        {
            // 同一天里，同一经纪人或同一房源的其他预约
            var sameDay = await _context.Showings
                .Where(s => s.ShowingDate == date
                            && (s.AgentId == agentId || s.ListingId == listingId)
                            && (ignoreShowingId == null || s.ShowingId != ignoreShowingId))
                .ToListAsync();

            // 全部换算成"当天第几分钟"来比较
            double newStart = start.ToTimeSpan().TotalMinutes;
            double newEnd = end.ToTimeSpan().TotalMinutes;

            foreach (var other in sameDay)
            {
                if (other.StartTime == null || other.EndTime == null)
                {
                    continue;
                }

                double otherStart = other.StartTime.Value.ToTimeSpan().TotalMinutes;
                double otherEnd = other.EndTime.Value.ToTimeSpan().TotalMinutes;
                string otherRange = $"{other.StartTime.Value:HH:mm}-{other.EndTime.Value:HH:mm}";

                // 经纪人冲突：两段时间（含前后各 15 分钟）有重叠。
                // 恰好间隔 15 分钟是允许的（用严格的 < 和 > 比较）
                if (other.AgentId == agentId
                    && otherStart < newEnd + TravelBufferMinutes
                    && otherEnd > newStart - TravelBufferMinutes)
                {
                    return $"The agent already has a showing from {otherRange} that day. " +
                           $"A {TravelBufferMinutes}-minute travel gap is required before and after each showing.";
                }

                // 房源冲突：同一房源同一时间被另一位经纪人带看
                if (other.ListingId == listingId
                    && other.AgentId != agentId
                    && otherStart < newEnd
                    && otherEnd > newStart)
                {
                    return $"This listing is already being shown by another agent from {otherRange} that day.";
                }
            }

            return null;
        }

        // Office Manager 可以改任何预约；经纪人只能改自己的预约（规则 014）
        private async Task<bool> CanEditShowingAsync(Showing showing)
        {
            if (User.IsInRole(AppRoles.OfficeManager))
            {
                return true;
            }

            Agent? me = await _currentAgentService.GetAsync(User);
            return me != null && showing.AgentId == me.AgentId;
        }

        // 准备表单需要的显示数据：客户、可选房源、经纪人列表、经纪人是否锁定
        private async Task PopulateFormAsync(ShowingFormViewModel model, Customer? customer)
        {
            await _listingService.ExpireOverdueListingsAsync();

            bool isManager = User.IsInRole(AppRoles.OfficeManager);
            Agent? me = await _currentAgentService.GetAsync(User);

            model.Customer = customer;

            model.Agents = await _context.Agents
                .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
                .ToListAsync();

            // 可选房源：状态为 Available 的；修改时把原来选的房源也保留在列表里
            int? currentListingId = model.Showing.ListingId;
            model.Listings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Available || l.ListingId == currentListingId)
                .OrderBy(l => l.StreetAddress)
                .ToListAsync();

            // 规则 016：经纪人自己预约时，经纪人自动选定且不能改；Office Manager 从列表里选
            model.AgentLocked = !isManager;
            if (!isManager)
            {
                model.Showing.AgentId = me?.AgentId;
            }
        }

        // =====================================================
        // 预约详情（保存成功后的确认页）
        // =====================================================

        // GET: Showings/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var showing = await _context.Showings
                .Include(s => s.Customer)
                .Include(s => s.Listing)
                .Include(s => s.Agent)
                .FirstOrDefaultAsync(s => s.ShowingId == id);
            if (showing == null)
            {
                return NotFound();
            }

            // 只有有权限的人才显示"Edit"按钮
            // （前台没有预约权限，所以一定不显示；其他角色再判断是不是本人的预约）
            bool canBook = User.IsInRole(AppRoles.OfficeManager)
                           || User.IsInRole(AppRoles.Broker)
                           || User.IsInRole(AppRoles.AgentRole);
            ViewBag.CanEdit = canBook && await CanEditShowingAsync(showing);

            return View(showing);
        }

        // =====================================================
        // 页面 6：预约搜索 + 结果列表
        // =====================================================

        // GET: Showings/Search?date=2026-11-20&agentId=3
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] ShowingSearchViewModel model)
        {
            model.Agents = await _context.Agents
                .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
                .ToListAsync();

            if (model.Searched)
            {
                // 日期格式不对时，模型绑定已经产生了错误；这里只补充"一个条件都没填"的情况
                if (model.Date == null && model.AgentId == null && ModelState.IsValid)
                {
                    ModelState.AddModelError(string.Empty, "Please enter a date and/or choose an agent.");
                }

                if (ModelState.IsValid)
                {
                    var query = _context.Showings
                        .Include(s => s.Customer)
                        .Include(s => s.Listing)
                        .Include(s => s.Agent)
                        .AsQueryable();

                    if (model.Date != null)
                    {
                        query = query.Where(s => s.ShowingDate == model.Date);
                    }
                    if (model.AgentId != null)
                    {
                        query = query.Where(s => s.AgentId == model.AgentId);
                    }

                    model.Results = await query
                        .OrderBy(s => s.ShowingDate).ThenBy(s => s.StartTime)
                        .ToListAsync();
                }
            }

            // 计算哪些预约当前用户可以点进去修改（只有 Office Manager 或相关经纪人）
            var editableIds = new HashSet<int>();
            if (User.IsInRole(AppRoles.OfficeManager))
            {
                editableIds = model.Results.Select(s => s.ShowingId).ToHashSet();
            }
            else if (User.IsInRole(AppRoles.Broker) || User.IsInRole(AppRoles.AgentRole))
            {
                Agent? me = await _currentAgentService.GetAsync(User);
                if (me != null)
                {
                    editableIds = model.Results
                        .Where(s => s.AgentId == me.AgentId)
                        .Select(s => s.ShowingId)
                        .ToHashSet();
                }
            }
            ViewBag.EditableIds = editableIds;

            return View(model);
        }
    }
}
