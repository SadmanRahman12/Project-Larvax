using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LarvaX.Core.Interfaces;
using Microsoft.Extensions.Options;
using LarvaX.Core.Options;

namespace LarvaX.Infrastructure.Services
{
    public class BkashClient : IBkashClient
    {
        private readonly HttpClient _http;
        private readonly BkashOptions _options;

        public BkashClient(HttpClient http, IOptions<BkashOptions> options)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _options = options?.Value ?? new BkashOptions();
        }

        public async Task<BkashPaymentResult> InitiatePaymentAsync(string mobileNumber, decimal amount, string merchantInvoiceId)
        {
            // Minimal, configurable implementation. Real bKash flow requires token exchange and payment create/execute.
            try
            {
                if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                {
                    return new BkashPaymentResult { Success = false, Message = "bKash BaseUrl not configured." };
                }

                // Example payload - APIs vary across bKash providers. Use configured endpoints where possible.
                var payload = new
                {
                    amount = amount.ToString("F2"),
                    invoice = merchantInvoiceId,
                    mobile = mobileNumber,
                    merchantId = _options.MerchantId
                };

                var endpoint = _options.CreatePaymentEndpoint ?? "create_payment";
                var url = CombineUrl(_options.BaseUrl, endpoint);

                var resp = await _http.PostAsJsonAsync(url, payload);
                var raw = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    return new BkashPaymentResult { Success = false, Message = $"Gateway returned {(int)resp.StatusCode}", RawResponse = raw };
                }

                // Try to parse a gateway transaction id from response. This is best-effort; adjust to real API response shape.
                using var doc = JsonDocument.Parse(raw);
                string? gatewayId = null;
                if (doc.RootElement.TryGetProperty("transactionId", out var t))
                {
                    gatewayId = t.GetString();
                }
                else if (doc.RootElement.TryGetProperty("trxId", out var t2))
                {
                    gatewayId = t2.GetString();
                }

                return new BkashPaymentResult
                {
                    Success = true,
                    GatewayTransactionId = gatewayId ?? $"BK-{Guid.NewGuid():N}",
                    Message = "Payment initiated (response parsed).",
                    RawResponse = raw
                };
            }
            catch (Exception ex)
            {
                return new BkashPaymentResult { Success = false, Message = ex.Message, RawResponse = ex.ToString() };
            }
        }

        private static string CombineUrl(string baseUrl, string endpoint)
        {
            if (string.IsNullOrEmpty(baseUrl)) return endpoint;
            if (baseUrl.EndsWith('/')) baseUrl = baseUrl[..^1];
            if (endpoint.StartsWith('/')) endpoint = endpoint[1..];
            return baseUrl + "/" + endpoint;
        }
    }
}
