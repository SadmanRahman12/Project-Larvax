using System.Threading.Tasks;
using LarvaX.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Controllers
{
    [ApiController]
    [Route("api/bkash/webhook")]
    public class BkashWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BkashWebhookController(ApplicationDbContext db)
        {
            _db = db;
        }

        public class BkashWebhookPayload
        {
            public string? GatewayTransactionId { get; set; }
            public string? TransactionReference { get; set; }
            public string? Status { get; set; }
            public decimal Amount { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] BkashWebhookPayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.GatewayTransactionId))
            {
                return BadRequest();
            }

            var txn = await _db.PaymentTransactions
                .FirstOrDefaultAsync(t => t.GatewayTransactionId == payload.GatewayTransactionId);

            if (txn == null)
            {
                // Try by transaction reference
                if (!string.IsNullOrWhiteSpace(payload.TransactionReference))
                {
                    txn = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.TransactionReference == payload.TransactionReference);
                }
            }

            if (txn == null)
            {
                return NotFound();
            }

            // Map status
            if (!string.IsNullOrWhiteSpace(payload.Status))
            {
                var s = payload.Status!.ToLowerInvariant();
                if (s == "completed" || s == "success" || s == "paid") txn.Status = PaymentStatus.Completed;
                else if (s == "failed" || s == "declined") txn.Status = PaymentStatus.Failed;
                else if (s == "pending") txn.Status = PaymentStatus.Pending;
            }

            txn.GatewayTransactionId = payload.GatewayTransactionId;
            txn.Amount = payload.Amount;
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
