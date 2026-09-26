using System;

namespace LarvaX.Core.Entities
{
    public class SubscriptionFeature
    {
        public int Id { get; set; }
        public int SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public string FeatureKey { get; set; } = null!; // e.g. "DenAiUnlimited", "SymptomTrends", "FamilyProfiles", "DoctorTelemedSuite", "OrgAnalytics"
        public string Name { get; set; } = null!;
        public string NameBn { get; set; } = null!;
        public string? Description { get; set; }
        public string? DescriptionBn { get; set; }
        public bool IsIncluded { get; set; } = true;
    }
}
