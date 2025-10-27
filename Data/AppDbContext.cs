using DevToolsSuite.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DevToolsSuite.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SavedTool> SavedTools { get; set; }
        public DbSet<ToolUsage> ToolUsages { get; set; }
        public DbSet<PaymentOrder> PaymentOrders { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SavedTool configuration
            builder.Entity<SavedTool>()
                .HasOne(st => st.User)
                .WithMany(u => u.SavedTools)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ToolUsage configuration
            builder.Entity<ToolUsage>()
                .HasIndex(tu => tu.UsedAt);

            builder.Entity<ToolUsage>()
                .HasIndex(tu => tu.ToolName);

            builder.Entity<ToolUsage>()
                .HasOne(tu => tu.User)
                .WithMany(u => u.ToolUsages)
                .HasForeignKey(tu => tu.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // PaymentOrder configuration
            builder.Entity<PaymentOrder>()
                .HasOne(po => po.User)
                .WithMany(u => u.PaymentOrders)
                .HasForeignKey(po => po.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentOrder>()
                .HasIndex(po => po.OrderId)
                .IsUnique();

            builder.Entity<PaymentOrder>()
                .HasIndex(po => po.CreatedAt);

            // UserSubscription configuration
            builder.Entity<UserSubscription>()
                .HasOne(us => us.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSubscription>()
                .HasIndex(us => us.SubscriptionId)
                .IsUnique();

            builder.Entity<UserSubscription>()
                .HasIndex(us => us.Status);
        }
    }
}