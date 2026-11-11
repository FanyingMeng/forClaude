using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ssw2026TermProject.Models
{
    // 【E2 新增】图片类型：页面 7 第一步要选
    public enum UploadImageType
    {
        [Display(Name = "Agent Photo")] AgentPhoto = 1,
        [Display(Name = "Listing Photo")] ListingPhoto = 2
    }

    public class UploadedImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UploadedImageId { get; set; }

        // 保存后的相对路径（E2 改动：未审核图片放在 tempImages，审核通过后才会挪到 approvedImages）
        [Required]
        public string? FilePath { get; set; }

        public string? Description { get; set; }

        public string? AltText { get; set; }

        public DateTime UploadDateTime { get; set; } = DateTime.Now;

        // 上传员工的 ID
        [Required]
        public string? StaffMemberId { get; set; }

        // 规则 012：图片默认未审核，网站上显示 "Picture Not Available"
        public bool IsApproved { get; set; } = false;

        // ---------- 以下为 E2 新增字段（都允许为空，不影响 E1 已有数据） ----------

        // 图片类型（经纪人照片 / 房源照片）
        public UploadImageType? ImageType { get; set; }

        // 最终保存的文件名（重名加数字后的名字，元数据也用这个名字）
        [StringLength(260)]
        public string? FileName { get; set; }

        // 关联的经纪人（经纪人照片时使用）
        public int? AgentId { get; set; }
        public Agent? Agent { get; set; }

        // 关联的房源（房源照片时使用）
        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        // 规则 006：每个房源最多显示 7 张已批准图片，这个标记表示"选中显示在房源页上"
        public bool DisplayOnListing { get; set; }

        // 审核预留字段（规则 017：审批人必须是另一位员工，审核功能本轮不做界面）
        public string? ApprovedByStaffId { get; set; }
        public DateTime? ApprovedDate { get; set; }

        // 规则 017 的校验方法：审批人不能为空，且不能是上传者本人
        // （方法不会被映射成数据库列，所以不需要任何特性）
        public bool IsEligibleApprover(string? approverStaffId) =>
            !string.IsNullOrEmpty(approverStaffId) && approverStaffId != StaffMemberId;
    }
}
