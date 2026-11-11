using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ssw2026TermProject.Models
{
    // 【E2 新增】预约看房实体（Save Showing 用例）
    // 预约 = 某客户，在某日期、某时间段，与某经纪人一起看某房源
    public class Showing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Showing ID")]
        public int ShowingId { get; set; }

        // 看房的客户
        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // 要看的房源（只能选状态为 Available 的）
        [Required(ErrorMessage = "Please select a listing.")]
        [Display(Name = "Listing")]
        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        // 带看的经纪人（规则 002：预约必须选经纪人）
        [Required(ErrorMessage = "Please select an agent.")]
        [Display(Name = "Agent")]
        public int? AgentId { get; set; }
        public Agent? Agent { get; set; }

        // 用 DateOnly / TimeOnly 分开保存日期和时间，方便做冲突判断
        [Required(ErrorMessage = "Date is required.")]
        [Display(Name = "Date")]
        public DateOnly? ShowingDate { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        [Display(Name = "Start Time")]
        public TimeOnly? StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [Display(Name = "End Time")]
        public TimeOnly? EndTime { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedByStaffId { get; set; }
    }
}
