using System;
using System.Collections.Generic;

namespace LarvaX.Core.Entities
{
    public enum PlanTier
    {
        CitizenFree = 0,
        CitizenPremium = 1,
        Professional = 2,
        Institutional = 3
    }

    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameBn { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string DescriptionBn { get; set; } = null!;
        public PlanTier Tier { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public string Currency { get; set; } = "BDT";
        public bool IsActive { get; set; } = true;
        public int MaxFamilyMembers { get; set; } = 1;
        
        // Quota limits (-1 = unlimited)
        public int DailyDenAiQuota { get; set; } = 5;
        
        public bool AllowSymptomTrends { get; set; } = false;
        public bool AllowPriorityConsultation { get; set; } = false;
        public bool AllowOrganizationDashboard { get; set; } = false;
        public bool AllowAdvancedAnalytics { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SubscriptionFeature> Features { get; set; } = new List<SubscriptionFeature>();
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}
