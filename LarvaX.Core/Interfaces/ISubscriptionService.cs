using System.Collections.Generic;
using System.Threading.Tasks;
using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public string? Code { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public interface ISubscriptionService
    {
        Task<List<SubscriptionPlan>> GetAllActivePlansAsync();
        Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
        Task<UserSubscription?> GetUserActiveSubscriptionAsync(string userId);
        Task<bool> HasFeatureAccessAsync(string? userId, string featureKey);
        Task<(bool Allowed, int Remaining, int Limit)> CheckDailyQuotaAsync(string? userId, string featureKey);
        Task RecordFeatureUsageAsync(string? userId, string featureKey);
        Task<UserSubscription> SubscribeUserAsync(string userId, int planId, BillingCycle cycle, PaymentMethod method, string transactionRef, decimal paidAmount, string? couponCode = null);
        Task<bool> CancelSubscriptionAsync(string userId);
        Task<List<Invoice>> GetUserInvoicesAsync(string userId);
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, string userId);
        Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId);
        Task<CouponValidationResult> ValidateCouponAsync(string code, decimal currentAmount);
        Task<List<FamilyProfile>> GetFamilyProfilesAsync(string userId);
        Task<FamilyProfile> AddFamilyProfileAsync(string userId, FamilyProfile profile);
        Task<bool> DeleteFamilyProfileAsync(string userId, int profileId);
        
        // Admin reporting metrics
        Task<(int TotalSubscribers, decimal TotalRevenue, int ActivePremium, int ActivePro)> GetSubscriptionStatsAsync();
        Task<List<PaymentTransaction>> GetAllRecentTransactionsAsync(int count = 20);
        Task<List<Coupon>> GetAllCouponsAsync();
        Task<Coupon> CreateCouponAsync(Coupon coupon);
    }
}
