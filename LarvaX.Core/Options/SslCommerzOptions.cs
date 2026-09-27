namespace LarvaX.Core.Options
{
    public class SslCommerzOptions
    {
        public string? BaseUrl { get; set; }
        public string? StoreId { get; set; }
        public string? StorePassword { get; set; }
        public string? CreatePaymentEndpoint { get; set; }
        public string? ValidatePaymentEndpoint { get; set; }
    }
}
