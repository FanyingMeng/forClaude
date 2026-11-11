using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ssw2026TermProject.Models
{
    // 【E2 新增】模拟发送的邮件记录
    // 文档里"给客户发邮件"还没确认是否真发，所以先把邮件内容存进数据库，
    // 演示时可以在房源详情页直接展示邮件内容；以后接入真实邮件服务时只需替换 IEmailService 的实现
    public class SimulatedEmail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SimulatedEmailId { get; set; }

        [Required]
        public string? ToAddress { get; set; }

        [Required]
        public string? Subject { get; set; }

        [Required]
        public string? Body { get; set; }

        public DateTime SentDate { get; set; } = DateTime.Now;

        // 这封邮件对应的房源（用于在房源详情页展示）
        public int? ListingId { get; set; }
    }
}
