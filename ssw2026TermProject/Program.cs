using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.Services;

namespace ssw2026TermProject
{
    public class Program
    {
        // E2：改成 async，因为启动时要异步初始化角色
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("FinalProjConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>() // E2 新增：启用角色（Office Manager / Broker / Agent / Front Desk）
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();

            // E2 新增：注册业务服务
            builder.Services.AddScoped<CurrentAgentService>();   // 当前登录账号 -> 经纪人记录
            builder.Services.AddScoped<ListingService>();        // 房源到期状态等后台逻辑
            builder.Services.AddScoped<CustomerSearchService>(); // 客户搜索（房源页、预约页共用）
            builder.Services.AddScoped<IEmailService, SimulatedEmailService>(); // 模拟发邮件，将来可替换成真实实现

            var app = builder.Build();

            // E2 新增：启动时初始化角色（开发环境还会创建演示账号）
            using (var scope = app.Services.CreateScope())
            {
                await DbSeeder.SeedAsync(scope.ServiceProvider, app.Environment.IsDevelopment());
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            await app.RunAsync();
        }
    }
}
