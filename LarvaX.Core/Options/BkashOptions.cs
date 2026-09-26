namespace LarvaX.Core.Options
{
    public class BkashOptions
    {
        public string? BaseUrl { get; set; }
        public string? AppKey { get; set; }
        public string? AppSecret { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? MerchantId { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? CreatePaymentEndpoint { get; set; }
        public string? ExecutePaymentEndpoint { get; set; }
    }
}
