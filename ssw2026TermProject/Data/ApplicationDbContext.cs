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
    }
}
