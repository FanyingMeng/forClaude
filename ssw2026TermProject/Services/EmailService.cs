using ssw2026TermProject.Data;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.Services
{
    // 【E2 新增】邮件服务接口
    // 现在只有"模拟发送"一个实现；文档确认需要真发邮件后，写一个新的实现类并在 Program.cs 里替换即可
    public interface IEmailService
    {
        // 合同签署后，给客户发邮件：包含房源编号和线上链接
        Task SendListingSignedEmailAsync(Customer customer, Listing listing, string listingUrl);
    }

    // 模拟发送：不真的发邮件，只把邮件内容存进 SimulatedEmails 表
    public class SimulatedEmailService(ApplicationDbContext context) : IEmailService
    {
        public async Task SendListingSignedEmailAsync(Customer customer, Listing listing, string listingUrl)
        {
            string customerName = $"{customer.FirstName} {customer.LastName}".Trim();

            var email = new SimulatedEmail
            {
                ToAddress = customer.Email,
                Subject = $"Your listing #{listing.ListingId} is now online",
                Body =
                    $"Dear {customerName},\n\n" +
                    $"Thank you for signing the listing contract with Oakridge Realty.\n" +
                    $"Your listing number is {listing.ListingId} ({listing.StreetAddress}, {listing.City}).\n" +
                    $"The contract runs from {listing.StartDate:yyyy-MM-dd} to {listing.EndDate:yyyy-MM-dd}.\n\n" +
                    $"You can view your listing online here:\n{listingUrl}\n\n" +
                    $"Oakridge Realty",
                SentDate = DateTime.Now,
                ListingId = listing.ListingId
            };

            context.SimulatedEmails.Add(email);
            await context.SaveChangesAsync();
        }
    }
}
