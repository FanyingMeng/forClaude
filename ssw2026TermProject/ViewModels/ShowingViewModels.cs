using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ssw2026TermProject.Models;

namespace ssw2026TermProject.ViewModels
{
    // 【E2 新增】页面 5：预约表单（新建和修改共用）
    public class ShowingFormViewModel
    {
        public Showing Showing { get; set; } = new();

        // ---------- 仅用于显示 ----------
        [ValidateNever]
        public Customer? Customer { get; set; }

        // 可选房源（状态 Available）
        [ValidateNever]
        public List<Listing> Listings { get; set; } = new();

        // 经纪人下拉列表（Office Manager 才能自由选）
        [ValidateNever]
        public List<Agent> Agents { get; set; } = new();

        // 规则 016：经纪人自己预约时，经纪人自动选定且不能改
        public bool AgentLocked { get; set; }

        public bool IsEdit { get; set; }
    }

    // 【E2 新增】页面 6：预约搜索
    public class ShowingSearchViewModel
    {
        // 按日期搜索（可选）
        public DateOnly? Date { get; set; }

        // 按经纪人搜索（可选）
        public int? AgentId { get; set; }

        public bool Searched { get; set; }

        [ValidateNever]
        public List<Showing> Results { get; set; } = new();

        [ValidateNever]
        public List<Agent> Agents { get; set; } = new();
    }

    // 共用组件：经纪人下拉选择（房源表单、预约表单、搜索页都用）
    public class AgentSelectModel
    {
        // 生成的 <select> 的 name，例如 "Listing.AgentId"
        public string FieldName { get; set; } = "AgentId";
        public int? SelectedId { get; set; }
        public List<Agent> Agents { get; set; } = new();

        // 锁定时下拉框禁用，同时输出一个隐藏域保证值仍会提交
        public bool Locked { get; set; }

        // 第一项的提示文字
        public string Placeholder { get; set; } = "-- Select an agent --";

        // 供 JS 使用的元素 id
        public string ElementId { get; set; } = "agentSelect";
    }
}
