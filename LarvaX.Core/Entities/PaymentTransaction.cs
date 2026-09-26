using System;

namespace LarvaX.Core.Entities
{
    public enum PaymentMethod
    {
        Bkash = 1,
        Nagad = 2,
        Card = 3,
        BankTransfer = 4,
        MockInstant = 5
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4
    }

    public class PaymentTransaction
    {
        public int Id { get; set; }

        public int? UserSubscriptionId { get; set; }
        public virtual UserSubscription? UserSubscription { get; set; }

        public string UserId { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BDT";

        public PaymentMethod Method { get; set; } = PaymentMethod.Bkash;
        public string TransactionReference { get; set; } = null!;
        public string? GatewayTransactionId { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
