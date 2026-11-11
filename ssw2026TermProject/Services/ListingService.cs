using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.Services
{
    // 【E2 新增】房源相关的后台逻辑
    public class ListingService(ApplicationDbContext context)
    {
        // 房源 3 个月到期后状态变化：把合同期已过的 Available 房源改成 Expired
        // 在每次读取房源之前调用，这样不需要额外的定时任务，页面上看到的状态永远是最新的
        public async Task ExpireOverdueListingsAsync()
        {
            DateTime today = DateTime.Today;

            var overdue = await context.Listings
                .Where(l => l.Status == ListingStatus.Available
                            && l.EndDate != null
                            && l.EndDate < today)
                .ToListAsync();

            if (overdue.Count == 0)
            {
                return;
            }

            foreach (var listing in overdue)
            {
                listing.Status = ListingStatus.Expired;
            }

            await context.SaveChangesAsync();
        }

        // 合同期内（已签、状态 Available）的房源不能更换经纪人（规则 003）
        public static bool IsContractActive(Listing listing) =>
            listing.ContractSigned && listing.Status == ListingStatus.Available;
    }
}
