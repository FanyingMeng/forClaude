using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.ViewModels;

namespace ssw2026TermProject.Services
{
    // 【E2 新增】客户搜索逻辑（房源页和预约页共用）
    // 搜索条件：姓、名、电话、客户编号，至少填一项；多项同时填写时必须全部匹配
    public class CustomerSearchService(ApplicationDbContext context)
    {
        public async Task SearchAsync(CustomerSearchViewModel model, ModelStateDictionary modelState)
        {
            string? first = model.FirstName?.Trim();
            string? last = model.LastName?.Trim();
            string? phone = model.Phone?.Trim();
            string? idText = model.CustomerId?.Trim();

            // 1. 条件不完整：一项都没填
            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last)
                && string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(idText))
            {
                modelState.AddModelError(string.Empty,
                    "Please enter at least one search condition (first name, last name, phone or customer ID).");
                return;
            }

            // 2. 条件无效：客户编号不是正整数
            int customerId = 0;
            if (!string.IsNullOrEmpty(idText) && (!int.TryParse(idText, out customerId) || customerId <= 0))
            {
                modelState.AddModelError(nameof(model.CustomerId), "Customer ID must be a positive number.");
            }

            // 3. 条件无效：电话至少要有 4 位数字（支持只输入后四位）
            string phoneDigits = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(phone) && phoneDigits.Length < 4)
            {
                modelState.AddModelError(nameof(model.Phone), "Please enter at least 4 digits of the phone number.");
            }

            if (!modelState.IsValid)
            {
                return;
            }

            // 先在数据库里按姓名和编号过滤
            var query = context.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(first))
            {
                query = query.Where(c => c.FirstName != null && c.FirstName.Contains(first));
            }
            if (!string.IsNullOrEmpty(last))
            {
                query = query.Where(c => c.LastName != null && c.LastName.Contains(last));
            }
            if (!string.IsNullOrEmpty(idText))
            {
                query = query.Where(c => c.ID == customerId);
            }

            var customers = await query
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .ToListAsync();

            // 电话在内存里按"纯数字"比较：库里的电话可能带括号、横线或空格，直接用 SQL 的 Contains 会漏掉
            // （客户数量不大，这样做足够，也最稳妥）
            if (!string.IsNullOrEmpty(phone))
            {
                customers = customers
                    .Where(c => new string((c.Phone ?? "").Where(char.IsDigit).ToArray()).Contains(phoneDigits))
                    .ToList();
            }

            model.Results = customers;
        }
    }
}
