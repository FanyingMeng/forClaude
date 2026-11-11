using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.Data
{
    // 【E2 新增】启动时初始化角色；开发环境下额外创建几个演示账号，方便演示和测试权限
    public static class DbSeeder
    {
        // 仅开发环境使用的演示密码（生产环境不会创建这些账号）
        private const string DevPassword = "Dev@12345";

        public static async Task SeedAsync(IServiceProvider services, bool isDevelopment)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            try
            {
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                // 1. 保证 4 个角色都存在
                foreach (string role in AppRoles.All)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                if (!isDevelopment)
                {
                    return;
                }

                // 2. 开发环境：创建演示账号（邮箱已确认，可直接登录）
                await EnsureUserAsync(userManager, "manager@oakridge.test", AppRoles.OfficeManager);
                await EnsureUserAsync(userManager, "broker@oakridge.test", AppRoles.Broker);
                await EnsureUserAsync(userManager, "agent@oakridge.test", AppRoles.AgentRole);
                await EnsureUserAsync(userManager, "frontdesk@oakridge.test", AppRoles.FrontDesk);

                // 3. 给 Broker 和 Agent 账号配上经纪人记录（Username 与登录名一致，预约时才能自动选中本人）
                await EnsureAgentAsync(context, "broker@oakridge.test", "Bruce", "Broker", "100000001");
                await EnsureAgentAsync(context, "agent@oakridge.test", "Alice", "Agent", "100000002");
            }
            catch (Exception ex)
            {
                // 数据库还没迁移时不要让网站直接崩溃，提示去执行迁移即可
                logger.LogWarning(ex, "Seeding roles/demo data failed. Have you run 'dotnet ef database update'?");
            }
        }

        private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager, string email, string role)
        {
            var user = await userManager.FindByNameAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, DevPassword);
                if (!result.Succeeded)
                {
                    return;
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        private static async Task EnsureAgentAsync(ApplicationDbContext context, string username, string firstName, string lastName, string sin)
        {
            if (await context.Agents.AnyAsync(a => a.Username == username))
            {
                return;
            }

            context.Agents.Add(new Agent
            {
                FirstName = firstName,
                LastName = lastName,
                Username = username,
                SIN = sin,
                DateOfBirth = new DateTime(1985, 1, 1),
                StreetAddress = "1 Demo Street",
                City = "Moncton",
                Province = "NB",
                PostalCode = "E1C 1A1",
                MobilePhone = "506-555-0101",
                OfficePhone = "506-555-0102",
                Email = username,
                HasVerifiedGovernmentId = true,
                DateOfEmployment = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }
}
