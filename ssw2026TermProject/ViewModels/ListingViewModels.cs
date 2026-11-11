using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.ViewModels
{
    // 【E2 新增】页面 2：客户的房源概览
    public class CustomerListingsViewModel
    {
        public Customer Customer { get; set; } = new();
        public List<Listing> Listings { get; set; } = new();
    }

    // 【E2 新增】页面 3：房源表单（新建和修改共用）
    public class ListingFormViewModel
    {
        // 表单里真正提交的房源数据
        public Listing Listing { get; set; } = new();

        // 页面上勾选的要显示在房源页上的图片 ID（最多 7 张）
        [ValidateNever]
        public List<int> SelectedImageIds { get; set; } = new();

        // ---------- 以下仅用于显示，不参与绑定和校验 ----------
        [ValidateNever]
        public Customer? Customer { get; set; }

        // 该房源已批准的图片，可供勾选
        [ValidateNever]
        public List<UploadedImage> ApprovedImages { get; set; } = new();

        // 该房源已上传但还没批准的图片数量（只提示，不能选）
        [ValidateNever]
        public int PendingImageCount { get; set; }

        // 经纪人下拉列表数据
        [ValidateNever]
        public List<Agent> Agents { get; set; } = new();

        public bool IsEdit { get; set; }

        // 规则 003：合同期内不能换经纪人，此时经纪人下拉框锁定
        public bool AgentLocked { get; set; }
    }

    // 【E2 新增】页面 4：房源详情 / 取消页
    public class ListingDetailsViewModel
    {
        public Listing Listing { get; set; } = new();

        // 已批准且被选中显示的图片
        public List<UploadedImage> DisplayImages { get; set; } = new();

        // 已上传但未批准的图片数量
        public int PendingImageCount { get; set; }

        // 模拟发送给客户的最近一封邮件（演示时展示）
        public SimulatedEmail? LatestEmail { get; set; }

        // 当前用户能不能取消（规则 008）
        public bool CanCancel { get; set; }

        // 能取消的前提下才有意义：是否因为角色不够而被拒绝
        public bool CancelBlockedByRole { get; set; }
    }

    // 【E2 新增】网站公开页：房源列表
    public class BrowseViewModel
    {
        public string? Query { get; set; }
        public List<Listing> Listings { get; set; } = new();

        // 每个房源的封面图（只取已批准且被选中的第一张）
        public Dictionary<int, UploadedImage> CoverImages { get; set; } = new();
    }

    // 【E2 新增】网站公开页：房源详情
    public class PublicPropertyViewModel
    {
        public Listing Listing { get; set; } = new();
        public List<UploadedImage> Images { get; set; } = new();
    }
}
