using Microsoft.AspNetCore.Identity;

namespace LarvaX.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? PreferredLanguage { get; set; } = "en"; // en or bn
        public string? ModePreference { get; set; } = "Citizen"; // Citizen or Professional
        
        // Professional roles require admin approval
        public bool IsApproved { get; set; } = false;
        
        // Set to true when admin explicitly rejects the account — blocks login permanently
        public bool IsRejected { get; set; } = false;
        
        public string? RejectionReason { get; set; }

        public string? Specialty { get; set; }
        public string? Address { get; set; }

        // Monetization & Subscription relations
        public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<SubscriptionUsage> SubscriptionUsages { get; set; } = new List<SubscriptionUsage>();
        public virtual ICollection<FamilyProfile> FamilyProfiles { get; set; } = new List<FamilyProfile>();
    }
}
