using Razorpay.Api;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DevToolsSuite.Data;
using DevToolsSuite.Models;

namespace DevToolsSuite.Services
{
    public interface IPaymentService
    {
        Task<string> CreateOrderAsync(decimal amount, string currency, string userId, string planId);
        Task<bool> VerifyPaymentAsync(string paymentId, string orderId, string signature);
        Task<bool> CreateSubscriptionAsync(string userId, string planId);
        Task<bool> CancelSubscriptionAsync(string subscriptionId, bool cancelAtCycleEnd = false);
        Task<PaymentDetails> GetPaymentDetailsAsync(string paymentId);
        Task<List<PaymentHistory>> GetPaymentHistoryAsync(string userId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly RazorpayClient _razorpayClient;
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _keySecret;

        public PaymentService(AppDbContext context, IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;

            var keyId = configuration["Razorpay:KeyId"];
            _keySecret = configuration["Razorpay:KeySecret"] ?? string.Empty;

            if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(_keySecret))
            {
                throw new InvalidOperationException("Razorpay KeyId and KeySecret must be configured");
            }

            _razorpayClient = new RazorpayClient(keyId, _keySecret);
        }

        public async Task<string> CreateOrderAsync(decimal amount, string currency, string userId, string planId)
        {
            try
            {
                // Validation
                if (amount <= 0)
                {
                    throw new ArgumentException("Amount must be greater than zero", nameof(amount));
                }

                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentException("User ID is required", nameof(userId));
                }

                var amountInPaise = (int)(amount * 100);

                var options = new Dictionary<string, object>
                {
                    { "amount", amountInPaise },
                    { "currency", currency },
                    { "receipt", $"order_{DateTime.UtcNow:yyyyMMddHHmmss}" },
                    { "notes", new Dictionary<string, string>
                        {
                            { "user_id", userId },
                            { "plan_id", planId }
                        }
                    }
                };

                var order = _razorpayClient.Order.Create(options);
                string orderId = order["id"]?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(orderId))
                {
                    throw new InvalidOperationException("Failed to create order: Order ID is null");
                }

                var paymentOrder = new PaymentOrder
                {
                    OrderId = orderId,
                    UserId = userId,
                    PlanId = planId,
                    Amount = amount,
                    Currency = currency,
                    Status = "created",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentOrders.Add(paymentOrder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created order {OrderId} for user {UserId}", orderId, userId);
                return orderId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> VerifyPaymentAsync(string paymentId, string orderId, string signature)
        {
            try
            {
                // Verify signature using HMAC SHA256
                var payload = $"{orderId}|{paymentId}";
                var expectedSignature = GenerateSignature(payload, _keySecret);

                if (signature != expectedSignature)
                {
                    _logger.LogWarning("Invalid payment signature for order {OrderId}", orderId);
                    return false;
                }

                var order = await _context.PaymentOrders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return false;
                }

                order.Status = "paid";
                order.PaymentId = paymentId;
                order.PaidAt = DateTime.UtcNow;

                var user = await _context.Users.FindAsync(order.UserId);
                if (user != null)
                {
                    user.SubscriptionPlan = order.PlanId;
                    user.SubscriptionExpiry = DateTime.UtcNow.AddMonths(1);

                    var subscription = new UserSubscription
                    {
                        UserId = user.Id,
                        SubscriptionId = $"sub_{paymentId}",
                        PlanId = order.PlanId,
                        Status = "active",
                        StartedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMonths(1)
                    };
                    _context.UserSubscriptions.Add(subscription);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment verified for order {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification failed for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> CreateSubscriptionAsync(string userId, string planId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                // Check for existing active subscription
                var existingSubscription = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

                if (existingSubscription != null)
                {
                    _logger.LogWarning("User {UserId} already has active subscription", userId);
                    return false;
                }

                var plan = GetPlanDetails(planId);
                if (string.IsNullOrEmpty(plan.RazorpayPlanId))
                {
                    _logger.LogWarning("Invalid plan {PlanId}", planId);
                    return false;
                }

                var options = new Dictionary<string, object>
                {
                    { "plan_id", plan.RazorpayPlanId },
                    { "total_count", 12 },
                    { "customer_notify", 1 },
                    { "notes", new Dictionary<string, string>
                        {
                            { "user_id", userId },
                            { "plan_name", plan.Name }
                        }
                    }
                };

                var subscription = _razorpayClient.Subscription.Create(options);
                string subscriptionId = subscription["id"]?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    throw new InvalidOperationException("Failed to create subscription");
                }

                var userSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionId = subscriptionId,
                    PlanId = planId,
                    Status = "active",
                    StartedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMonths(1)
                };

                _context.UserSubscriptions.Add(userSubscription);

                user.SubscriptionPlan = planId;
                user.SubscriptionExpiry = DateTime.UtcNow.AddMonths(1);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription {SubscriptionId} for user {UserId}", subscriptionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(string subscriptionId, bool cancelAtCycleEnd = false)
        {
            try
            {
                var userSubscription = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

                if (userSubscription == null)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                    return false;
                }

                if (cancelAtCycleEnd)
                {
                    // Mark subscription to cancel at cycle end in DB
                    userSubscription.Status = "cancel_pending";
                    // Optionally, track the cycle end date
                    userSubscription.CancelAtCycleEnd = true;

                    _logger.LogInformation("Subscription {SubscriptionId} will be cancelled at cycle end", subscriptionId);
                }
                else
                {
                    // Cancel immediately in Razorpay
                    _razorpayClient.Subscription.Fetch(subscriptionId).Cancel();

                    userSubscription.Status = "cancelled";
                    userSubscription.CancelledAt = DateTime.UtcNow;

                    // Update user subscription info
                    var user = await _context.Users.FindAsync(userSubscription.UserId);
                    if (user != null)
                    {
                        user.SubscriptionPlan = "Free";
                        user.SubscriptionExpiry = DateTime.MinValue;
                    }

                    _logger.LogInformation("Cancelled subscription {SubscriptionId} immediately", subscriptionId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        public Task<PaymentDetails> GetPaymentDetailsAsync(string paymentId)
        {
            try
            {
                var payment = _razorpayClient.Payment.Fetch(paymentId);

                var details = new PaymentDetails
                {
                    PaymentId = paymentId,
                    Amount = Convert.ToDecimal(payment["amount"]) / 100,
                    Currency = payment["currency"]?.ToString() ?? "INR",
                    Status = payment["status"]?.ToString() ?? string.Empty,
                    Method = payment["method"]?.ToString() ?? string.Empty,
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(payment["created_at"])).DateTime
                };

                return Task.FromResult(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment details for {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<List<PaymentHistory>> GetPaymentHistoryAsync(string userId)
        {
            return await _context.PaymentOrders
                .Where(o => o.UserId == userId && o.Status == "paid")
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new PaymentHistory
                {
                    OrderId = o.OrderId,
                    Amount = o.Amount,
                    Currency = o.Currency,
                    PlanId = o.PlanId,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    PaidAt = o.PaidAt
                })
                .ToListAsync();
        }

        private PlanDetails GetPlanDetails(string planId)
        {
            var plans = new Dictionary<string, PlanDetails>
            {
                { "pro", new PlanDetails
                    {
                        Name = "Pro",
                        RazorpayPlanId = _configuration["Razorpay:PlanIds:Pro"] ?? string.Empty
                    }
                },
                { "team", new PlanDetails
                    {
                        Name = "Team",
                        RazorpayPlanId = _configuration["Razorpay:PlanIds:Team"] ?? string.Empty
                    }
                }
            };

            return plans.GetValueOrDefault(planId.ToLower()) ?? new PlanDetails();
        }

        private string GenerateSignature(string payload, string secret)
        {
            var encoding = new System.Text.UTF8Encoding();
            var keyBytes = encoding.GetBytes(secret);
            var payloadBytes = encoding.GetBytes(payload);

            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    public class PaymentDetails
    {
        public string PaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentHistory
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PlanId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class PlanDetails
    {
        public string Name { get; set; } = string.Empty;
        public string RazorpayPlanId { get; set; } = string.Empty;
    }
}