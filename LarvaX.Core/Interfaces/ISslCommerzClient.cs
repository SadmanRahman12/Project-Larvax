using System.Threading.Tasks;

namespace LarvaX.Core.Interfaces
{
    public class SslCommerzPaymentResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? Message { get; set; }
        public string? RawResponse { get; set; }
    }

    public interface ISslCommerzClient
    {
        Task<SslCommerzPaymentResult> CreatePaymentAsync(decimal amount, string currency, string transactionId, string successUrl, string failUrl, string cancelUrl);
        Task<SslCommerzPaymentResult> ValidatePaymentAsync(string transactionId);
    }
}
