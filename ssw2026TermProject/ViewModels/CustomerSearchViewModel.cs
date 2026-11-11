using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ssw2026TermProject.Models;
using System.ComponentModel.DataAnnotations;

namespace ssw2026TermProject.ViewModels
{
    // 【E2 新增】客户搜索表单 + 结果（房源页和预约页共用，对应共用组件"客户搜索部分视图"）
    public class CustomerSearchViewModel
    {
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        // 客户编号用字符串接收，这样输入 "abc" 时我们能给出友好的错误提示
        [Display(Name = "Customer ID")]
        public string? CustomerId { get; set; }

        // 表单提交时带上，用来区分"刚打开页面"和"点了搜索"
        public bool Searched { get; set; }

        // ---------- 以下不由表单绑定，由控制器填写 ----------

        // 搜索结果
        [ValidateNever]
        public List<Customer> Results { get; set; } = new();

        // 搜索表单提交到哪个控制器/动作（GET 提交）
        [ValidateNever]
        public string FormController { get; set; } = "Listings";
        [ValidateNever]
        public string FormAction { get; set; } = "SearchCustomer";

        // 点击"选择"后跳转到哪个控制器/动作（会带上 customerId 参数）
        [ValidateNever]
        public string SelectController { get; set; } = "Listings";
        [ValidateNever]
        public string SelectAction { get; set; } = "CustomerListings";
    }
}
