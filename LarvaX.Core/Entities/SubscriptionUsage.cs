using System;

namespace LarvaX.Core.Entities
{
    public class SubscriptionUsage
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;

        public string FeatureKey { get; set; } = null!;
        public DateTime UsageDate { get; set; } // Stored as UTC date
        public int Count { get; set; } = 1;
    }
}
