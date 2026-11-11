using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Agent> Agents { get; set; }


        public DbSet<UploadedImage> UploadedImages { get; set; }

        public DbSet<Customer> Customers { get; set; }

        // ---------- 以下为 E2 新增的表 ----------

        // 房源（Save Listing）
        public DbSet<Listing> Listings { get; set; }

        // 预约看房（Save Showing）
        public DbSet<Showing> Showings { get; set; }

        // 模拟发送的邮件记录
        public DbSet<SimulatedEmail> SimulatedEmails { get; set; }

        // 【E2 新增】配置外键的删除行为
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Identity 自带的表配置必须先调用基类
            base.OnModelCreating(modelBuilder);

            // 全部使用 Restrict（有关联数据时禁止删除），
            // 一来避免 SQL Server 的"多条级联路径"错误，二来保证房源/预约/图片不会因为误删客户或经纪人而丢失
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Customer).WithMany()
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Agent).WithMany()
                .HasForeignKey(l => l.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Showing>()
                .HasOne(s => s.Customer).WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Showing>()
                .HasOne(s => s.Listing).WithMany()
                .HasForeignKey(s => s.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Showing>()
                .HasOne(s => s.Agent).WithMany()
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UploadedImage>()
                .HasOne(i => i.Agent).WithMany()
                .HasForeignKey(i => i.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UploadedImage>()
                .HasOne(i => i.Listing).WithMany(l => l.Images)
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
