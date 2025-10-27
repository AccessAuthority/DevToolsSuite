using DevToolsSuite.Data;
using DevToolsSuite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System.Xml;

namespace DevToolsSuite.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/[action]")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await GetDashboardStatsAsync();
            return View(stats);
        }

        public async Task<IActionResult> Users(string search = "", int page = 1, int pageSize = 20)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.Email.Contains(search) || 
                    u.DisplayName.Contains(search) ||
                    u.SubscriptionPlan.Contains(search));
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    SubscriptionPlan = u.SubscriptionPlan,
                    SubscriptionExpiry = u.SubscriptionExpiry,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    EmailConfirmed = u.EmailConfirmed
                })
                .ToListAsync();

            var model = new UserListViewModel
            {
                Users = users,
                Search = search,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalUsers
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserPlan(string userId, string plan)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.SubscriptionPlan = plan;
            user.SubscriptionExpiry = plan == "Free" ? DateTime.UtcNow : DateTime.UtcNow.AddYears(1);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Updated user {UserId} plan to {Plan}", userId, plan);
                TempData["Success"] = $"User plan updated to {plan} successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user plan.";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent admin from deleting themselves
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted user {UserId}", userId);
                TempData["Success"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> ToolUsage(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var usageStats = await _context.ToolUsages
                .Where(u => u.UsedAt >= startDate && u.UsedAt <= endDate)
                .GroupBy(u => u.ToolName)
                .Select(g => new ToolUsageStats
                {
                    ToolName = g.Key,
                    UsageCount = g.Count(),
                    UniqueUsers = g.Select(u => u.UserId).Distinct().Count(),
                    LastUsed = g.Max(u => u.UsedAt),
                    AvgProcessingTime = g.Average(u => u.ProcessingTimeMs)
                })
                .OrderByDescending(s => s.UsageCount)
                .ToListAsync();

            var totalUsage = await _context.ToolUsages
                .Where(u => u.UsedAt >= startDate && u.UsedAt <= endDate)
                .CountAsync();

            var model = new ToolUsageViewModel
            {
                UsageStats = usageStats,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                TotalUsage = totalUsage
            };

            return View(model);
        }

        public async Task<IActionResult> Payments(int page = 1, int pageSize = 20)
        {
            var payments = await _context.PaymentOrders
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentViewModel
                {
                    OrderId = p.OrderId,
                    UserEmail = p.User.Email,
                    PlanId = p.PlanId,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt
                })
                .ToListAsync();

            var totalPayments = await _context.PaymentOrders.CountAsync();
            var totalRevenue = await _context.PaymentOrders
                .Where(p => p.Status == "paid")
                .SumAsync(p => p.Amount);

            var model = new PaymentListViewModel
            {
                Payments = payments,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalPayments,
                TotalRevenue = totalRevenue
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SystemHealth()
        {
            var health = new SystemHealthViewModel
            {
                ServerTime = DateTime.UtcNow,
                DatabaseStatus = await CheckDatabaseHealthAsync(),
                MemoryUsage = GetMemoryUsage(),
                ActiveUsers = await GetActiveUsersCountAsync(),
                Uptime = GetUptime(),
                CpuUsage = GetCpuUsage()
            };
            return View(health);
        }

        [HttpPost]
        public async Task<IActionResult> ExportToolUsage(DateTime startDate, DateTime endDate, string format = "csv")
        {
            var usageStats = await _context.ToolUsages
                .Where(u => u.UsedAt >= startDate && u.UsedAt <= endDate)
                .GroupBy(u => u.ToolName)
                .Select(g => new 
                {
                    ToolName = g.Key,
                    UsageCount = g.Count(),
                    UniqueUsers = g.Select(u => u.UserId).Distinct().Count(),
                    AvgProcessingTime = g.Average(u => u.ProcessingTimeMs)
                })
                .ToListAsync();

            if (format == "json")
            {
                var json = JsonConvert.SerializeObject(usageStats, Newtonsoft.Json.Formatting.Indented);
                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"tool-usage-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.json");
            }
            else
            {
                var csv = new StringBuilder();
                csv.AppendLine("ToolName,UsageCount,UniqueUsers,AvgProcessingTime");
                foreach (var stat in usageStats)
                {
                    csv.AppendLine($"{stat.ToolName},{stat.UsageCount},{stat.UniqueUsers},{stat.AvgProcessingTime:F2}");
                }
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"tool-usage-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.csv");
            }
        }

        private async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddMonths(-1);

            return new DashboardStats
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                ActiveToday = await _context.ToolUsages
                    .Where(u => u.UsedAt.Date == now.Date)
                    .Select(u => u.UserId)
                    .Distinct()
                    .CountAsync(),
                ProUsers = await _userManager.Users.CountAsync(u => u.SubscriptionPlan == "Pro"),
                TeamUsers = await _userManager.Users.CountAsync(u => u.SubscriptionPlan == "Team"),
                TotalToolUses = await _context.ToolUsages.CountAsync(),
                ToolUsesThisMonth = await _context.ToolUsages
                    .Where(u => u.UsedAt.Month == now.Month && u.UsedAt.Year == now.Year)
                    .CountAsync(),
                MonthlyRevenue = await _context.PaymentOrders
                    .Where(p => p.PaidAt.HasValue && 
                               p.PaidAt.Value.Month == now.Month && 
                               p.PaidAt.Value.Year == now.Year)
                    .SumAsync(p => p.Amount),
                PendingPayments = await _context.PaymentOrders
                    .CountAsync(p => p.Status == "created")
            };
        }

        private async Task<bool> CheckDatabaseHealthAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            return $"{(process.WorkingSet64 / 1024 / 1024):N0} MB";
        }

        private async Task<int> GetActiveUsersCountAsync()
        {
            var activeThreshold = DateTime.UtcNow.AddMinutes(-30);
            return await _context.ToolUsages
                .Where(u => u.UsedAt >= activeThreshold)
                .Select(u => u.UserId)
                .Distinct()
                .CountAsync();
        }

        private TimeSpan GetUptime()
        {
            return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
        private string GetCpuUsage()
        {
            throw new NotImplementedException();
        }
    }

    // View Models
    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveToday { get; set; }
        public int ProUsers { get; set; }
        public int TeamUsers { get; set; }
        public int TotalToolUses { get; set; }
        public int ToolUsesThisMonth { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PendingPayments { get; set; }
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

    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class ToolUsageStats
    {
        public string ToolName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int UniqueUsers { get; set; }
        public DateTime LastUsed { get; set; }
        public double AvgProcessingTime { get; set; }
    }

    public class ToolUsageViewModel
    {
        public List<ToolUsageStats> UsageStats { get; set; } = new();
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
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class SystemHealthViewModel
    {
        public DateTime ServerTime { get; set; }
        public bool DatabaseStatus { get; set; }
        public string MemoryUsage { get; set; } = string.Empty;
        public string CpuUsage { get; set; } = string.Empty;
        public int ActiveUsers { get; set; }
        public TimeSpan Uptime { get; set; }
    }
}