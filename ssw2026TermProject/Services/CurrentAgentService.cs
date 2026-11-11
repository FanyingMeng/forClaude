using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.Models;
using System.Security.Claims;

namespace ssw2026TermProject.Services
{
    // 【E2 新增】根据当前登录的账号，找到对应的经纪人记录
    // 约定：Agent.Username（或 Agent.Email）与登录账号的用户名一致
    // 用途：规则 016——经纪人自己预约时，系统自动选中他本人
    public class CurrentAgentService(ApplicationDbContext context)
    {
        public async Task<Agent?> GetAsync(ClaimsPrincipal user)
        {
            string? loginName = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(loginName))
            {
                return null;
            }

            // SQL Server 默认不区分大小写，所以大小写不同也能匹配上
            return await context.Agents.FirstOrDefaultAsync(a =>
                a.Username == loginName || a.Email == loginName);
        }
    }
}
