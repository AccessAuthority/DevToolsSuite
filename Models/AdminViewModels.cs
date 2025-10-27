namespace DevToolsSuite.Models.ViewModels
{
    public class DashboardStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveToday { get; set; }
        public int ProUsers { get; set; }
        public int TeamUsers { get; set; }
        public int TotalToolUses { get; set; }
        public int ToolUsesThisMonth { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PendingPayments { get; set; }
        public int NewUsersThisWeek { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? SubscriptionPlan { get; set; }
        public DateTime SubscriptionExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public bool EmailConfirmed { get; set; }
    }

    public class ToolUsageStatsViewModel
    {
        public string ToolName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int UniqueUsers { get; set; }
        public DateTime LastUsed { get; set; }
        public double AvgProcessingTime { get; set; }
    }

    public class ToolUsageViewModel
    {
        public List<ToolUsageStatsViewModel> UsageStats { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalUsage { get; set; }
    }

    public class PaymentViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class PaymentListViewModel
    {
        public List<PaymentViewModel> Payments { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; } = 20;
        public int TotalCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class SystemHealthViewModel
    {
        public DateTime ServerTime { get; set; }
        public bool DatabaseStatus { get; set; }
        public string MemoryUsage { get; set; } = string.Empty;
        public int ActiveUsers { get; set; }
        public TimeSpan Uptime { get; set; }
        public double CpuUsage { get; set; }
    }
}