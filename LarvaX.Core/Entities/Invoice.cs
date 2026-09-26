using System;

namespace LarvaX.Core.Entities
{
    public enum InvoiceStatus
    {
        Paid = 1,
        Unpaid = 2,
        Void = 3,
        Refunded = 4
    }

    public class Invoice
    {
        public int Id { get; set; }

        public int? UserSubscriptionId { get; set; }
        public virtual UserSubscription? UserSubscription { get; set; }

        public string UserId { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }

        public string InvoiceNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BDT";

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Paid;

        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? PaidDate { get; set; }

        public string? PlanName { get; set; }
        public string? Notes { get; set; }
    }
}
