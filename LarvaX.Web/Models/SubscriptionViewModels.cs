using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class PricingViewModel
    {
        public List<SubscriptionPlan> Plans { get; set; } = new();
        public UserSubscription? CurrentSubscription { get; set; }
        public bool IsAuthenticated { get; set; }
        public string CurrentLanguage { get; set; } = "en";
    }

    public class CheckoutViewModel
    {
        [Required]
        public int PlanId { get; set; }
        public SubscriptionPlan? Plan { get; set; }

        public BillingCycle Cycle { get; set; } = BillingCycle.Monthly;
        public decimal BasePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }

        public string? CouponCode { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Bkash;

        // Mobile Wallet fields
        [Display(Name = "Wallet Mobile Number")]
        public string? MobileNumber { get; set; }

        [Display(Name = "PIN / OTP")]
        public string? PinOrOtp { get; set; }

        // Card fields
        [Display(Name = "Cardholder Name")]
        public string? CardHolderName { get; set; }

        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiration (MM/YY)")]
        public string? CardExpiry { get; set; }

        [Display(Name = "CVV")]
        public string? CardCvv { get; set; }

        public string CurrentLanguage { get; set; } = "en";
    }

    public class MySubscriptionViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public UserSubscription? ActiveSubscription { get; set; }
        public SubscriptionPlan CurrentPlan { get; set; } = null!;
        
        // Quota indicators
        public int DenAiLimit { get; set; }
        public int DenAiUsedToday { get; set; }
        public int DenAiRemaining { get; set; }
        public bool DenAiIsUnlimited { get; set; }

        // Family members
        public List<FamilyProfile> FamilyProfiles { get; set; } = new();
        public int MaxFamilyAllowed { get; set; }

        // Invoices and transactions
        public List<Invoice> Invoices { get; set; } = new();
        public List<PaymentTransaction> Transactions { get; set; } = new();

        public string CurrentLanguage { get; set; } = "en";
    }

    public class CouponCheckRequest
    {
        public string Code { get; set; } = null!;
        public decimal BaseAmount { get; set; }
    }

    public class FamilyMemberAddRequest
    {
        [Required]
        public string FullName { get; set; } = null!;
        
        [Required]
        public string Relationship { get; set; } = "Spouse";
        
        public int? Age { get; set; }
        public string? BloodGroup { get; set; }
        public string? KnownAllergies { get; set; }
        public string? MedicalNotes { get; set; }
    }
}
