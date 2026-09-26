using System.Threading.Tasks;

namespace LarvaX.Core.Interfaces
{
    public class BkashPaymentResult
    {
        public bool Success { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? Message { get; set; }
        public string? RawResponse { get; set; }
    }

    public interface IBkashClient
    {
        /// <summary>
        /// Initiates a bKash payment for the specified mobile wallet and amount.
        /// Returns a BkashPaymentResult containing gateway transaction id on success.
        /// </summary>
        Task<BkashPaymentResult> InitiatePaymentAsync(string mobileNumber, decimal amount, string merchantInvoiceId);
    }
}
