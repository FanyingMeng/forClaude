using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.ViewModels
{
    // 【E2 新增】页面 7 第一步：选择图片类型
    public class UploadTypeViewModel
    {
        public UploadImageType? ImageType { get; set; }
    }

    // 【E2 新增】页面 7 第二步：搜索并选择要关联的对象（房源或经纪人）
    public class UploadTargetViewModel
    {
        public UploadImageType ImageType { get; set; }

        // 房源：按客户名搜索；经纪人：按经纪人名搜索
        public string? Name { get; set; }

        public bool Searched { get; set; }

        // 用户在结果里选中的那一条（房源 ID 或经纪人 ID）
        public int? SelectedId { get; set; }

        [ValidateNever]
        public List<Listing> Listings { get; set; } = new();

        [ValidateNever]
        public List<Agent> Agents { get; set; } = new();
    }

    // 【E2 新增】页面 7 第三步：选文件并填写元数据
    public class UploadFileViewModel
    {
        public UploadImageType ImageType { get; set; }
        public int TargetId { get; set; }

        // 页面顶部显示"正在给谁上传"
        public string? TargetLabel { get; set; }

        // 自定义文件名（不含扩展名）；留空则使用原文件名
        public string? CustomFileName { get; set; }

        public string? Description { get; set; }
        public string? AltText { get; set; }
    }
}
