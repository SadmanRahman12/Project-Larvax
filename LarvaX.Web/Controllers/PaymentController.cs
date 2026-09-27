using Microsoft.AspNetCore.Mvc;
using LarvaX.Web.Models;

namespace LarvaX.Web.Controllers
{
    public class PaymentController : Controller
    {
        [HttpGet]
        public IActionResult BkashMock(string refId, decimal amount = 0, int planId = 0, string planName = "")
        {
            var vm = new BkashMockViewModel
            {
                TransactionReference = refId ?? string.Empty,
                Amount = amount,
                PlanId = planId,
                PlanName = planName
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BkashMockConfirm(string transactionReference, decimal amount, string mobileNumber)
        {
            // For mock: mark payment completed in DB if needed; here just redirect to confirmation
            TempData["InfoMessage"] = "Mock payment completed for " + mobileNumber;
            return RedirectToAction("Confirmation", "Subscription", new { refId = transactionReference });
        }
    }
}
