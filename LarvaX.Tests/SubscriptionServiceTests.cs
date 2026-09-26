using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LarvaX.Tests
{
    public class SubscriptionServiceTests
    {
        private ApplicationDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private async Task SeedPlansAsync(ApplicationDbContext db)
        {
            var freePlan = new SubscriptionPlan
            {
                Id = 1,
                Name = "Citizen Free",
                NameBn = "নাগরিক ফ্রি",
                Description = "Essential dengue safety",
                DescriptionBn = "প্রয়োজনীয় ডেঙ্গু সুরক্ষা",
                Tier = PlanTier.CitizenFree,
                PriceMonthly = 0m,
                PriceYearly = 0m,
                DailyDenAiQuota = 5,
                MaxFamilyMembers = 1,
                IsActive = true,
                Features = new List<SubscriptionFeature>
                {
                    new() { FeatureKey = "DenAiBasic", Name = "5 AI queries/day", NameBn = "৫টি এআই প্রশ্ন", IsIncluded = true }
                }
            };

            var premiumPlan = new SubscriptionPlan
            {
                Id = 2,
                Name = "Citizen Premium",
                NameBn = "নাগরিক প্রিমিয়াম",
                Description = "Unlimited AI & family profiles",
                DescriptionBn = "সীমাহীন এআই ও পরিবার",
                Tier = PlanTier.CitizenPremium,
                PriceMonthly = 199m,
                PriceYearly = 1999m,
                DailyDenAiQuota = -1,
                MaxFamilyMembers = 6,
                AllowSymptomTrends = true,
                IsActive = true,
                Features = new List<SubscriptionFeature>
                {
                    new() { FeatureKey = "DenAiUnlimited", Name = "Unlimited consultations", NameBn = "সীমাহীন পরামর্শ", IsIncluded = true },
                    new() { FeatureKey = "FamilyProfiles", Name = "6 family profiles", NameBn = "৬টি পরিবার প্রোফাইল", IsIncluded = true }
                }
            };

            var coupon = new Coupon
            {
                Id = 1,
                Code = "SAVE20",
                DiscountPercent = 20m,
                IsActive = true,
                MaxUses = 100,
                TimesUsed = 0
            };

            db.SubscriptionPlans.AddRange(freePlan, premiumPlan);
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllActivePlans_ReturnsConfiguredPlans()
        {
            var db = GetInMemoryContext("PlansTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            var plans = await service.GetAllActivePlansAsync();

            Assert.Equal(2, plans.Count);
            Assert.Contains(plans, p => p.Tier == PlanTier.CitizenFree);
            Assert.Contains(plans, p => p.Tier == PlanTier.CitizenPremium);
        }

        [Fact]
        public async Task ValidateCoupon_ValidCoupon_CalculatesDiscountCorrectly()
        {
            var db = GetInMemoryContext("CouponValidTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            var result = await service.ValidateCouponAsync("SAVE20", 200m);

            Assert.True(result.IsValid);
            Assert.Equal(40m, result.DiscountAmount);
            Assert.Equal(160m, result.FinalAmount);
        }

        [Fact]
        public async Task ValidateCoupon_InvalidCoupon_ReturnsFalse()
        {
            var db = GetInMemoryContext("CouponInvalidTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            var result = await service.ValidateCouponAsync("NONEXISTENT", 200m);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task SubscribeUser_CreatesActiveSubscription_AndInvoice()
        {
            var db = GetInMemoryContext("SubscribeTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            var sub = await service.SubscribeUserAsync(
                "user-1",
                2,
                BillingCycle.Monthly,
                PaymentMethod.Bkash,
                "BKS-TEST-001",
                199m);

            Assert.NotNull(sub);
            Assert.Equal(SubscriptionStatus.Active, sub.Status);
            Assert.Equal("user-1", sub.UserId);
            Assert.Equal(2, sub.SubscriptionPlanId);

            var invoices = await service.GetUserInvoicesAsync("user-1");
            Assert.Single(invoices);
            Assert.Equal(199m, invoices[0].Amount);

            var txns = await service.GetUserTransactionsAsync("user-1");
            Assert.Single(txns);
            Assert.Equal("BKS-TEST-001", txns[0].TransactionReference);
        }

        [Fact]
        public async Task Quota_FreeUser_LimitsTo5QueriesPerDay()
        {
            var db = GetInMemoryContext("QuotaFreeTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            // Initially has 5 remaining
            var (allowed1, rem1, lim1) = await service.CheckDailyQuotaAsync("free-user", "DenAi");
            Assert.True(allowed1);
            Assert.Equal(5, rem1);

            // Record 5 usages
            for (int i = 0; i < 5; i++)
            {
                await service.RecordFeatureUsageAsync("free-user", "DenAi");
            }

            // Now limit is reached
            var (allowed2, rem2, lim2) = await service.CheckDailyQuotaAsync("free-user", "DenAi");
            Assert.False(allowed2);
            Assert.Equal(0, rem2);
        }

        [Fact]
        public async Task Quota_PremiumUser_HasUnlimitedQueries()
        {
            var db = GetInMemoryContext("QuotaPremiumTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            // Subscribe user to Citizen Premium
            await service.SubscribeUserAsync(
                "prem-user",
                2,
                BillingCycle.Monthly,
                PaymentMethod.MockInstant,
                "LVX-TEST-002",
                199m);

            // Check quota
            var (allowed, rem, lim) = await service.CheckDailyQuotaAsync("prem-user", "DenAi");
            Assert.True(allowed);
            Assert.Equal(-1, lim); // -1 = Unlimited
        }

        [Fact]
        public async Task CancelSubscription_MarksCancelAtPeriodEnd()
        {
            var db = GetInMemoryContext("CancelSubTestDb");
            await SeedPlansAsync(db);
            var service = new SubscriptionService(db);

            await service.SubscribeUserAsync("user-cancel", 2, BillingCycle.Monthly, PaymentMethod.Card, "CRD-001", 199m);

            var cancelResult = await service.CancelSubscriptionAsync("user-cancel");
            Assert.True(cancelResult);

            var activeSub = await service.GetUserActiveSubscriptionAsync("user-cancel");
            Assert.NotNull(activeSub);
            Assert.True(activeSub.CancelAtPeriodEnd);
            Assert.False(activeSub.AutoRenew);
        }

        [Fact]
        public async Task PaymentService_ProcessSubscriptionPayment_SucceedsWithBkash()
        {
            var db = GetInMemoryContext("PaymentServiceTestDb");
            await SeedPlansAsync(db);
            var subService = new SubscriptionService(db);
            var payService = new PaymentService(subService, db);

            var request = new PaymentProcessRequest
            {
                UserId = "user-bkash",
                PlanId = 2,
                Cycle = BillingCycle.Monthly,
                Method = PaymentMethod.Bkash,
                MobileNumber = "01712345678",
                PinOrOtp = "1234",
                CouponCode = "SAVE20"
            };

            var result = await payService.ProcessSubscriptionPaymentAsync(request);

            Assert.True(result.Success);
            Assert.NotNull(result.TransactionReference);
            Assert.StartsWith("BKS-", result.TransactionReference);

            // Final amount should be 199 - 20% = 159.20
            var txn = await payService.GetTransactionByRefAsync(result.TransactionReference);
            Assert.NotNull(txn);
            Assert.Equal(159.20m, txn.Amount);
        }

        [Fact]
        public async Task FamilyProfile_AddAndRemove_WorksCorrectly()
        {
            var db = GetInMemoryContext("FamilyProfileTestDb");
            var service = new SubscriptionService(db);

            var profile = new FamilyProfile
            {
                FullName = "Ayesha Rahman",
                Relationship = "Spouse",
                Age = 32,
                BloodGroup = "O+"
            };

            var added = await service.AddFamilyProfileAsync("family-user", profile);
            Assert.True(added.Id > 0);

            var list = await service.GetFamilyProfilesAsync("family-user");
            Assert.Single(list);
            Assert.Equal("Ayesha Rahman", list[0].FullName);

            var deleted = await service.DeleteFamilyProfileAsync("family-user", added.Id);
            Assert.True(deleted);

            var listAfter = await service.GetFamilyProfilesAsync("family-user");
            Assert.Empty(listAfter);
        }
    }
}
