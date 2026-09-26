using System;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _db;

        public PaymentService(ISubscriptionService subscriptionService, ApplicationDbContext db)
        {
            _subscriptionService = subscriptionService;
            _db = db;
        }

        public async Task<PaymentProcessResult> ProcessSubscriptionPaymentAsync(PaymentProcessRequest request)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(request.PlanId);
            if (plan == null)
            {
                return new PaymentProcessResult
                {
                    Success = false,
                    Message = "Selected subscription plan does not exist."
                };
            }

            decimal baseAmount = request.Cycle == BillingCycle.Yearly ? plan.PriceYearly : plan.PriceMonthly;
            decimal finalAmount = baseAmount;

            // Apply coupon if given
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var couponResult = await _subscriptionService.ValidateCouponAsync(request.CouponCode, baseAmount);
                if (couponResult.IsValid)
                {
                    finalAmount = couponResult.FinalAmount;
                }
            }

            // Gateway validations
            if (request.Method == PaymentMethod.Bkash || request.Method == PaymentMethod.Nagad)
            {
                if (string.IsNullOrWhiteSpace(request.MobileNumber) || request.MobileNumber.Length < 11)
                {
                    return new PaymentProcessResult
                    {
                        Success = false,
                        Message = $"Please provide a valid 11-digit {(request.Method == PaymentMethod.Bkash ? "bKash" : "Nagad")} wallet number."
                    };
                }
            }
            else if (request.Method == PaymentMethod.Card)
            {
                if (string.IsNullOrWhiteSpace(request.CardNumber) || request.CardNumber.Replace(" ", "").Length < 16)
                {
                    return new PaymentProcessResult
                    {
                        Success = false,
                        Message = "Please enter a valid 16-digit debit/credit card number."
                    };
                }
            }

            // Generate clean gateway transaction reference
            string prefix = request.Method switch
            {
                PaymentMethod.Bkash => "BKS",
                PaymentMethod.Nagad => "NGD",
                PaymentMethod.Card => "CRD",
                PaymentMethod.BankTransfer => "BNK",
                _ => "LVX"
            };
            string txnRef = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            try
            {
                var subscription = await _subscriptionService.SubscribeUserAsync(
                    request.UserId,
                    request.PlanId,
                    request.Cycle,
                    request.Method,
                    txnRef,
                    finalAmount,
                    request.CouponCode);

                var txn = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.TransactionReference == txnRef);

                return new PaymentProcessResult
                {
                    Success = true,
                    Message = $"Payment of ৳{finalAmount:N0} successfully processed via {request.Method}!",
                    TransactionReference = txnRef,
                    GatewayTransactionId = txn?.GatewayTransactionId,
                    Transaction = txn,
                    Subscription = subscription
                };
            }
            catch (Exception ex)
            {
                return new PaymentProcessResult
                {
                    Success = false,
                    Message = $"Payment processing failed: {ex.Message}"
                };
            }
        }

        public async Task<PaymentTransaction?> GetTransactionByRefAsync(string reference)
        {
            return await _db.PaymentTransactions
                .Include(t => t.UserSubscription)
                .ThenInclude(s => s!.SubscriptionPlan)
                .FirstOrDefaultAsync(t => t.TransactionReference == reference);
        }
    }
}
