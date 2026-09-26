using System;
using System.Collections.Generic;

namespace LarvaX.Core.Entities
{
    public enum SubscriptionStatus
    {
        Active = 1,
        PastDue = 2,
        Canceled = 3,
        Expired = 4,
        Trialing = 5
    }

    public enum BillingCycle
    {
        Monthly = 1,
        Yearly = 2
    }

    public class UserSubscription
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;

        public int SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }

        public bool AutoRenew { get; set; } = true;
        public bool CancelAtPeriodEnd { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
