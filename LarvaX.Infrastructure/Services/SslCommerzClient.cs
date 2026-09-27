using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LarvaX.Core.Interfaces;
using LarvaX.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace LarvaX.Infrastructure.Services
{
    public class SslCommerzClient : ISslCommerzClient
    {
        private readonly HttpClient _http;
        private readonly SslCommerzOptions _options;
        private readonly ILogger<SslCommerzClient>? _logger;

        public SslCommerzClient(HttpClient http, IOptions<SslCommerzOptions> options, ILogger<SslCommerzClient>? logger = null)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _options = options?.Value ?? new SslCommerzOptions();
            _logger = logger;
        }

        public async Task<SslCommerzPaymentResult> CreatePaymentAsync(decimal amount, string currency, string transactionId, string successUrl, string failUrl, string cancelUrl)
        {
            try
            {
                var baseUrl = _options.BaseUrl?.TrimEnd('/') ?? string.Empty;
                var endpoint = _options.CreatePaymentEndpoint ?? "create_payment";
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return new SslCommerzPaymentResult { Success = false, Message = "SSLCOMMERZ base URL not configured." };
                }

                var url = baseUrl + (endpoint.StartsWith('/') ? endpoint : "/" + endpoint);

                var payload = new
                {
                    store_id = _options.StoreId,
                    store_passwd = _options.StorePassword,
                    total_amount = amount.ToString("F2"),
                    currency = currency,
                    tran_id = transactionId,
                    success_url = successUrl,
                    fail_url = failUrl,
                    cancel_url = cancelUrl
                };

                var resp = await _http.PostAsJsonAsync(url, payload);
                var raw = await resp.Content.ReadAsStringAsync();
                _logger?.LogDebug("SslCommerz Create response: {resp}", raw);

                if (!resp.IsSuccessStatusCode)
                {
                    return new SslCommerzPaymentResult { Success = false, Message = $"SSLCOMMERZ returned {(int)resp.StatusCode}", RawResponse = raw };
                }

                // Try to extract redirect_url or gateway_url from response
                using var doc = JsonDocument.Parse(raw);
                string? redirect = null;
                if (doc.RootElement.TryGetProperty("redirect_url", out var r1)) redirect = r1.GetString();
                else if (doc.RootElement.TryGetProperty("GatewayPageURL", out var r2)) redirect = r2.GetString();

                string? gt = null;
                if (doc.RootElement.TryGetProperty("tran_id", out var t)) gt = t.GetString();

                return new SslCommerzPaymentResult { Success = true, RedirectUrl = redirect, GatewayTransactionId = gt, RawResponse = raw, Message = "Created" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SslCommerz CreatePaymentAsync failed");
                return new SslCommerzPaymentResult { Success = false, Message = ex.Message, RawResponse = ex.ToString() };
            }
        }

        public Task<SslCommerzPaymentResult> ValidatePaymentAsync(string transactionId)
        {
            // For now, server-side validation can be implemented as needed.
            return Task.FromResult(new SslCommerzPaymentResult { Success = false, Message = "Not implemented" });
        }
    }
}
