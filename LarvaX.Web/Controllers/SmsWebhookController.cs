using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    [ApiController]
    [Route("api/sms")]
    public class SmsWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SmsWebhookController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Accepts a generic incoming SMS webhook payload.
        // The body parameters mirror the common Twilio/generic SMS webhook format.
        [HttpPost]
        public async Task<IActionResult> Receive([FromForm] string From, [FromForm] string Body)
        {
            if (string.IsNullOrWhiteSpace(From) || string.IsNullOrWhiteSpace(Body))
                return BadRequest("Missing sender number or message body.");

            var command = Body.Trim().ToUpperInvariant();
            string response;

            // Simple keyword-based command parser
            if (command.StartsWith("REPORT"))
                response = "Your dengue hazard report has been received. A health worker will follow up. Thank you for helping your community. - LarvaX";
            else if (command.StartsWith("DONOR"))
                response = "Blood donor lookup by SMS is not yet available. Please visit LarvaX at http://larvax.gov.bd or call 16000. - LarvaX";
            else if (command.StartsWith("HELP") || command == "?")
                response = "LarvaX SMS Commands: REPORT <location> to report a hazard. DONOR to find blood donors. STATUS to check alerts. Reply HELP for this menu.";
            else if (command.StartsWith("STATUS"))
                response = "Current dengue alert: MODERATE. Please avoid stagnant water and use mosquito protection. For emergencies call 999. - LarvaX";
            else
                response = "Unknown command. Reply HELP for a list of available LarvaX SMS commands.";

            var smsLog = new SmsCommand
            {
                SenderNumber = From,
                CommandText = Body,
                ReceivedDate = DateTime.UtcNow,
                Processed = true,
                ResponseMessage = response
            };

            _db.SmsCommands.Add(smsLog);
            await _db.SaveChangesAsync();

            // Return TwiML-style response (compatible with Twilio and most SMS gateways)
            return Content($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response><Message>{response}</Message></Response>",
                "application/xml");
        }

        [HttpGet("logs")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrator")]
        public IActionResult Logs(int page = 1, int pageSize = 50)
        {
            var logs = _db.SmsCommands
                .OrderByDescending(s => s.ReceivedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(logs);
        }
    }
}
