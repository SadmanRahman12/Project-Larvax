using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _db;

        public SubscriptionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<SubscriptionPlan>> GetAllActivePlansAsync()
        {
            return await _db.SubscriptionPlans
                .Include(p => p.Features)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Tier)
                .ToListAsync();
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
        {
            return await _db.SubscriptionPlans
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.Id == planId);
        }

        public async Task<UserSubscription?> GetUserActiveSubscriptionAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var now = DateTime.UtcNow;
            return await _db.UserSubscriptions
                .Include(s => s.SubscriptionPlan)
                .ThenInclude(p => p.Features)
                .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active && s.EndDate >= now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasFeatureAccessAsync(string? userId, string featureKey)
        {
            // If user has an active paid subscription
            if (!string.IsNullOrEmpty(userId))
            {
                var activeSub = await GetUserActiveSubscriptionAsync(userId);
                if (activeSub != null)
                {
                    // Higher tiers have access to all lower tier features
                    if (activeSub.SubscriptionPlan.Tier >= PlanTier.CitizenPremium)
                    {
                        if (featureKey == "DenAiUnlimited" || 
                            featureKey == "SymptomTrends" || 
                            featureKey == "FamilyProfiles" ||
                            featureKey == "PriorityAlerts" ||
                            featureKey == "HydrationReminders" ||
                            featureKey == "ExportHealthSummary")
                        {
                            return true;
                        }
                    }

                    if (activeSub.SubscriptionPlan.Tier >= PlanTier.Professional)
                    {
                        if (featureKey == "DoctorTelemedSuite" ||
                            featureKey == "DoctorAnalytics" ||
                            featureKey == "PriorityConsultation")
                        {
                            return true;
                        }
                    }

                    if (activeSub.SubscriptionPlan.Tier >= PlanTier.Institutional)
                    {
                        return true;
                    }

                    // Check explicit features list
                    var match = activeSub.SubscriptionPlan.Features.FirstOrDefault(f => f.FeatureKey == featureKey && f.IsIncluded);
                    if (match != null) return true;
                }
            }

            // Fallback to Citizen Free plan capabilities
            var freePlan = await _db.SubscriptionPlans
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.Tier == PlanTier.CitizenFree && p.IsActive);

            if (freePlan != null)
            {
                var freeFeature = freePlan.Features.FirstOrDefault(f => f.FeatureKey == featureKey && f.IsIncluded);
                return freeFeature != null;
            }

            return false;
        }

        public async Task<(bool Allowed, int Remaining, int Limit)> CheckDailyQuotaAsync(string? userId, string featureKey)
        {
            var today = DateTime.UtcNow.Date;

            // Free default daily limit
            int limit = 5;

            if (!string.IsNullOrEmpty(userId))
            {
                var activeSub = await GetUserActiveSubscriptionAsync(userId);
                if (activeSub != null)
                {
                    // Premium / Pro users have unlimited or very high quota
                    if (activeSub.SubscriptionPlan.Tier >= PlanTier.CitizenPremium)
                    {
                        return (true, 999999, -1);
                    }
                    limit = activeSub.SubscriptionPlan.DailyDenAiQuota;
                }
            }

            var queryUserId = string.IsNullOrEmpty(userId) ? "anonymous" : userId;
            var usage = await _db.SubscriptionUsages
                .FirstOrDefaultAsync(u => u.UserId == queryUserId && u.FeatureKey == featureKey && u.UsageDate == today);

            int currentCount = usage?.Count ?? 0;
            int remaining = Math.Max(0, limit - currentCount);
            bool allowed = remaining > 0;

            return (allowed, remaining, limit);
        }

        public async Task RecordFeatureUsageAsync(string? userId, string featureKey)
        {
            var queryUserId = string.IsNullOrEmpty(userId) ? "anonymous" : userId;
            var today = DateTime.UtcNow.Date;

            var usage = await _db.SubscriptionUsages
                .FirstOrDefaultAsync(u => u.UserId == queryUserId && u.FeatureKey == featureKey && u.UsageDate == today);

            if (usage == null)
            {
                usage = new SubscriptionUsage
                {
                    UserId = queryUserId,
                    FeatureKey = featureKey,
                    UsageDate = today,
                    Count = 1
                };
                _db.SubscriptionUsages.Add(usage);
            }
            else
            {
                usage.Count++;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<UserSubscription> SubscribeUserAsync(
            string userId, 
            int planId, 
            BillingCycle cycle, 
            PaymentMethod method, 
            string transactionRef, 
            decimal paidAmount, 
            string? couponCode = null,
            string? gatewayTransactionId = null)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(planId)
                       ?? throw new InvalidOperationException($"Subscription plan with ID {planId} not found.");

            var now = DateTime.UtcNow;
            var duration = cycle == BillingCycle.Yearly ? TimeSpan.FromDays(365) : TimeSpan.FromDays(30);
            var endDate = now.Add(duration);

            // Deactivate any currently active subscriptions for this user
            var existingSubs = await _db.UserSubscriptions
                .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                .ToListAsync();

            foreach (var s in existingSubs)
            {
                s.Status = SubscriptionStatus.Expired;
                s.UpdatedAt = now;
            }

            var newSub = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                BillingCycle = cycle,
                StartDate = now,
                EndDate = endDate,
                NextBillingDate = endDate,
                AutoRenew = true,
                CancelAtPeriodEnd = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.UserSubscriptions.Add(newSub);
            await _db.SaveChangesAsync();

            // Record Payment Transaction
            var txn = new PaymentTransaction
            {
                UserSubscriptionId = newSub.Id,
                UserId = userId,
                Amount = paidAmount,
                Currency = plan.Currency,
                Method = method,
                TransactionReference = transactionRef,
                GatewayTransactionId = string.IsNullOrWhiteSpace(gatewayTransactionId)
                    ? $"GW-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}"
                    : gatewayTransactionId,
                Status = PaymentStatus.Completed,
                CreatedAt = now
            };
            _db.PaymentTransactions.Add(txn);

            // Record Invoice
            var invoiceNum = $"INV-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(10000, 99999)}";
            var invoice = new Invoice
            {
                UserSubscriptionId = newSub.Id,
                UserId = userId,
                InvoiceNumber = invoiceNum,
                Amount = paidAmount,
                Currency = plan.Currency,
                Status = InvoiceStatus.Paid,
                BillingPeriodStart = now,
                BillingPeriodEnd = endDate,
                IssueDate = now,
                PaidDate = now,
                PlanName = plan.Name,
                Notes = $"Subscription to {plan.Name} ({cycle})" + (!string.IsNullOrWhiteSpace(couponCode) ? $" [Coupon: {couponCode}]" : "")
            };
            _db.Invoices.Add(invoice);

            // Update Coupon usage if applicable
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper());
                if (coupon != null)
                {
                    coupon.TimesUsed++;
                }
            }

            await _db.SaveChangesAsync();
            return newSub;
        }

        public async Task<bool> CancelSubscriptionAsync(string userId)
        {
            var activeSub = await GetUserActiveSubscriptionAsync(userId);
            if (activeSub == null) return false;

            activeSub.AutoRenew = false;
            activeSub.CancelAtPeriodEnd = true;
            activeSub.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Invoice>> GetUserInvoicesAsync(string userId)
        {
            return await _db.Invoices
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, string userId)
        {
            return await _db.Invoices
                .Include(i => i.User)
                .Include(i => i.UserSubscription)
                .ThenInclude(s => s!.SubscriptionPlan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId);
        }

        public async Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId)
        {
            return await _db.PaymentTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<CouponValidationResult> ValidateCouponAsync(string code, decimal currentAmount)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new CouponValidationResult { IsValid = false, Message = "Please enter a coupon code." };
            }

            var cleanCode = code.Trim().ToUpperInvariant();
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanCode && c.IsActive);

            if (coupon == null)
            {
                return new CouponValidationResult { IsValid = false, Message = "Invalid or expired promo coupon code." };
            }

            if (coupon.ValidUntil.HasValue && coupon.ValidUntil.Value < DateTime.UtcNow)
            {
                return new CouponValidationResult { IsValid = false, Message = "This promo coupon has expired." };
            }

            if (coupon.TimesUsed >= coupon.MaxUses)
            {
                return new CouponValidationResult { IsValid = false, Message = "This coupon has reached its maximum usage limit." };
            }

            decimal discount = 0;
            if (coupon.DiscountPercent > 0)
            {
                discount = Math.Round(currentAmount * (coupon.DiscountPercent / 100m), 2);
            }
            else if (coupon.FixedDiscountAmount > 0)
            {
                discount = Math.Min(currentAmount, coupon.FixedDiscountAmount);
            }

            decimal finalAmount = Math.Max(0, currentAmount - discount);

            return new CouponValidationResult
            {
                IsValid = true,
                Code = coupon.Code,
                DiscountPercent = coupon.DiscountPercent,
                DiscountAmount = discount,
                FinalAmount = finalAmount,
                Message = $"Coupon '{coupon.Code}' applied successfully! Saved ৳{discount:N0}."
            };
        }

        public async Task<List<FamilyProfile>> GetFamilyProfilesAsync(string userId)
        {
            return await _db.FamilyProfiles
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<FamilyProfile> AddFamilyProfileAsync(string userId, FamilyProfile profile)
        {
            profile.UserId = userId;
            profile.CreatedAt = DateTime.UtcNow;
            _db.FamilyProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> DeleteFamilyProfileAsync(string userId, int profileId)
        {
            var item = await _db.FamilyProfiles.FirstOrDefaultAsync(f => f.Id == profileId && f.UserId == userId);
            if (item == null) return false;

            _db.FamilyProfiles.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(int TotalSubscribers, decimal TotalRevenue, int ActivePremium, int ActivePro)> GetSubscriptionStatsAsync()
        {
            var activeSubs = await _db.UserSubscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow)
                .ToListAsync();

            var totalSubs = activeSubs.Count;
            var activePremium = activeSubs.Count(s => s.SubscriptionPlan.Tier == PlanTier.CitizenPremium);
            var activePro = activeSubs.Count(s => s.SubscriptionPlan.Tier >= PlanTier.Professional);

            var totalRevenue = await _db.PaymentTransactions
                .Where(t => t.Status == PaymentStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            return (totalSubs, totalRevenue, activePremium, activePro);
        }

        public async Task<List<PaymentTransaction>> GetAllRecentTransactionsAsync(int count = 20)
        {
            return await _db.PaymentTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Coupon>> GetAllCouponsAsync()
        {
            return await _db.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Coupon> CreateCouponAsync(Coupon coupon)
        {
            coupon.Code = coupon.Code.Trim().ToUpperInvariant();
            coupon.CreatedAt = DateTime.UtcNow;
            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();
            return coupon;
        }
    }
}
