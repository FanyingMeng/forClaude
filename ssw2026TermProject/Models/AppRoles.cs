namespace ssw2026TermProject.Models
{
    // 【E2 新增】系统里的员工角色常量，集中放在这里，避免在各个控制器里手写字符串
    public static class AppRoles
    {
        // 办公室经理：可以代任何经纪人预约、创建/修改员工账号
        public const string OfficeManager = "OfficeManager";

        // 经纪长（Broker）：可以取消已分配经纪人的房源、创建/修改员工账号
        public const string Broker = "Broker";

        // 经纪人：只能为自己预约看房
        public const string AgentRole = "Agent";

        // 前台：可以录入房源、上传图片，但不能预约看房
        public const string FrontDesk = "FrontDesk";

        // 规则 011：能创建/修改员工账号的角色（用于 [Authorize(Roles = ...)]）
        public const string OfficeManagerOrBroker = OfficeManager + "," + Broker;

        // 规则 014：能创建预约的角色（经纪人只能约自己，在控制器里再限制）
        public const string CanBookShowings = OfficeManager + "," + Broker + "," + AgentRole;

        // 启动时要保证存在的全部角色
        public static readonly string[] All =
        {
            OfficeManager, Broker, AgentRole, FrontDesk
        };
    }
}
