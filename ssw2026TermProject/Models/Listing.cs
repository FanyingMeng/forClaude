using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ssw2026TermProject.Models
{
    // 【E2 新增】取暖方式（页面 3 的下拉框）
    public enum HeatingType
    {
        [Display(Name = "Electric")] Electric,
        [Display(Name = "Natural Gas")] NaturalGas,
        [Display(Name = "Heat Pump")] HeatPump,
        [Display(Name = "Oil Furnace")] OilFurnace,
        [Display(Name = "Wood Stove")] WoodStove,
        [Display(Name = "Geothermal")] Geothermal
    }

    // 【E2 新增】房源所在区域
    public enum CityArea
    {
        [Display(Name = "West End")] WestEnd,
        [Display(Name = "North End")] NorthEnd,
        [Display(Name = "South End")] SouthEnd,
        [Display(Name = "East End")] EastEnd,
        [Display(Name = "Downtown")] Downtown,
        [Display(Name = "Other")] Other
    }

    // 【E2 新增】房源状态：只有 Available 的房源才会显示在网站上
    public enum ListingStatus
    {
        [Display(Name = "Not Available")] NotAvailable = 0, // 合同未签
        [Display(Name = "Available")] Available = 1,        // 合同已签且在合同期内
        [Display(Name = "Expired")] Expired = 2,            // 3 个月合同期已过
        [Display(Name = "Cancelled")] Cancelled = 3         // 已被取消
    }

    // 【E2 新增】房源实体（Save Listing 用例）
    public class Listing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Listing ID")]
        public int ListingId { get; set; }

        // 房源所属客户（房东），必须是系统里已有的客户
        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // ---------- 地址 ----------
        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(150)]
        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(80)]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(40)]
        [Display(Name = "Province")]
        public string? Province { get; set; }

        // 邮编可以不填，填了就必须是加拿大邮编格式（与 Customer 一致）
        [Display(Name = "Postal Code")]
        [RegularExpression(@"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z][\s-]?\d[ABCEGHJ-NPRSTV-Z]\d$", ErrorMessage = "Please enter a valid postal code Eg: A1A 1A1")]
        public string? PostalCode { get; set; }

        // ---------- 房屋信息 ----------
        [Required(ErrorMessage = "Area (square feet) is required.")]
        [Range(100, 100000, ErrorMessage = "Area must be between 100 and 100,000 square feet.")]
        [Display(Name = "Area (sq ft)")]
        public int? SquareFeet { get; set; }

        [Required(ErrorMessage = "Number of bedrooms is required.")]
        [Range(0, 20, ErrorMessage = "Bedrooms must be between 0 and 20.")]
        [Display(Name = "Bedrooms")]
        public int? Bedrooms { get; set; }

        [Required(ErrorMessage = "Number of bathrooms is required.")]
        [Range(0, 20, ErrorMessage = "Bathrooms must be between 0 and 20.")]
        [Display(Name = "Bathrooms")]
        public int? Bathrooms { get; set; }

        [Required(ErrorMessage = "Number of floors is required.")]
        [Range(1, 10, ErrorMessage = "Floors must be between 1 and 10.")]
        [Display(Name = "Floors")]
        public int? Floors { get; set; }

        [Display(Name = "Neighbourhood")]
        public CityArea Area { get; set; }

        [StringLength(1000)]
        [Display(Name = "Features Description")]
        public string? FeatureDescription { get; set; }

        [Display(Name = "Heating Type")]
        public HeatingType HeatingType { get; set; }

        // ---------- 附加设施 ----------
        [Display(Name = "Fireplace")]
        public bool HasFireplace { get; set; }

        // 车库车位数：0 表示没有车库，最多 3 个
        [Range(0, 3, ErrorMessage = "Garage can have 0 to 3 spaces.")]
        [Display(Name = "Garage Spaces")]
        public int GarageSpaces { get; set; }

        [Display(Name = "Baby Barn")]
        public bool HasBabyBarn { get; set; }

        [Display(Name = "Central Air")]
        public bool HasCentralAir { get; set; }

        [Display(Name = "Screens (windows & doors)")]
        public bool HasScreens { get; set; }

        // ---------- 价格 / 合同 ----------
        [Required(ErrorMessage = "Asking price is required.")]
        [Range(1, 1000000000, ErrorMessage = "Please enter a valid asking price.")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Asking Price")]
        public decimal? AskingPrice { get; set; }

        // 合同是否已签（规则 002 / 003 / 007 的核心开关）
        [Display(Name = "Contract Signed")]
        public bool ContractSigned { get; set; }

        // 合同已签时才有经纪人（规则 002）；一个房源只能有一个经纪人（规则 003）
        [Display(Name = "Agent")]
        public int? AgentId { get; set; }
        public Agent? Agent { get; set; }

        // 合同起止日期：起始 = 签约当天，结束 = 3 个月后；未签合同时为空
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Status")]
        public ListingStatus Status { get; set; } = ListingStatus.NotAvailable;

        // ---------- 审计信息 ----------
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedByStaffId { get; set; }

        // 取消房源（规则 008）时记录时间和操作人
        public DateTime? CancelledDate { get; set; }
        public string? CancelledByStaffId { get; set; }

        // 与该房源关联的图片（页面 7 上传时关联）
        public ICollection<UploadedImage> Images { get; set; } = new List<UploadedImage>();
    }
}
