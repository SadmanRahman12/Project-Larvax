using System.Threading.Tasks;
using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    public class PaymentProcessRequest
    {
        public string UserId { get; set; } = null!;
        public int PlanId { get; set; }
        public BillingCycle Cycle { get; set; } = BillingCycle.Monthly;
        public PaymentMethod Method { get; set; } = PaymentMethod.Bkash;
        public decimal Amount { get; set; }
        public string? MobileNumber { get; set; }
        public string? PinOrOtp { get; set; }
        public string? CardNumber { get; set; }
        public string? CardCvv { get; set; }
        public string? CardExpiry { get; set; }
        public string? CouponCode { get; set; }
    }

    public class PaymentProcessResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? TransactionReference { get; set; }
        public string? GatewayTransactionId { get; set; }
        public PaymentTransaction? Transaction { get; set; }
        public UserSubscription? Subscription { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentProcessResult> ProcessSubscriptionPaymentAsync(PaymentProcessRequest request);
        Task<PaymentTransaction?> GetTransactionByRefAsync(string reference);
    }
}
