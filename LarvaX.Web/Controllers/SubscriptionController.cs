using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using Microsoft.EntityFrameworkCore;
using LarvaX.Core.Interfaces;
using LarvaX.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LarvaX.Infrastructure.Data.ApplicationDbContext _db;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IPaymentService paymentService,
            UserManager<ApplicationUser> userManager,
            LarvaX.Infrastructure.Data.ApplicationDbContext db,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _subscriptionService = subscriptionService;
            _paymentService = paymentService;
            _userManager = userManager;
            _db = db;
            _config = config;
        }

        // ==========================================
        // 6. Pay with bKash via SSLCOMMERZ redirect
        // ==========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithSslCommerz(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var plan = await _subscriptionService.GetPlanByIdAsync(model.PlanId);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "Selected plan not found.";
                return RedirectToAction(nameof(Index));
            }

            decimal amount = model.FinalPrice;
            var now = DateTime.UtcNow;

            // Create a transaction reference embedding planId and cycle so callback can restore context
            var gid = Guid.NewGuid().ToString("N");
            var txnRef = $"BKS-P{plan.Id}-C{(int)model.Cycle}-{now:yyyyMMdd}-{gid.Substring(0,8).ToUpperInvariant()}";

            var txn = new LarvaX.Core.Entities.PaymentTransaction
            {
                UserId = user.Id,
                Amount = amount,
                Currency = plan.Currency,
                Method = PaymentMethod.Bkash,
                TransactionReference = txnRef,
                Status = PaymentStatus.Pending,
                CreatedAt = now
            };

            _db.PaymentTransactions.Add(txn);
            await _db.SaveChangesAsync();

            // Build redirect to SSLCOMMERZ (simple GET with query string for demo). Configure in appsettings or user-secrets.
            var baseUrl = _config["SslCommerz:BaseUrl"] ?? "https://sandbox.sslcommerz.com/bkash/pay";
            var storeId = _config["SslCommerz:StoreId"] ?? string.Empty;
            var storePass = _config["SslCommerz:StorePassword"] ?? string.Empty;

            var returnUrl = Url.Action("SslCommerzCallback", "Subscription", new { refId = txnRef }, Request.Scheme);

            var redirectUrl = $"{baseUrl}?store_id={Uri.EscapeDataString(storeId)}&store_passwd={Uri.EscapeDataString(storePass)}&tran_id={Uri.EscapeDataString(txnRef)}&total_amount={amount}&currency={plan.Currency}&success_url={Uri.EscapeDataString(returnUrl)}&fail_url={Uri.EscapeDataString(returnUrl)}&cancel_url={Uri.EscapeDataString(returnUrl)}";

            return Redirect(redirectUrl);
        }

        // Minimal callback endpoint that SSLCOMMERZ will redirect to after payment attempt
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> SslCommerzCallback(string refId)
        {
            if (string.IsNullOrWhiteSpace(refId)) return RedirectToAction(nameof(Index));

            var txn = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.TransactionReference == refId);
            if (txn == null) return RedirectToAction(nameof(Index));

            // Map common query params returned by gateway
            var status = Request.Query["status"].ToString(); // e.g., VALID, FAILED
            var gatewayId = Request.Query["bank_tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(status) && status.Equals("VALID", StringComparison.OrdinalIgnoreCase))
            {
                txn.Status = PaymentStatus.Completed;
                if (!string.IsNullOrWhiteSpace(gatewayId)) txn.GatewayTransactionId = gatewayId;
                await _db.SaveChangesAsync();

                // Extract planId and cycle from refId (format: BKS-P{planId}-C{cycle}-...)
                try
                {
                    var parts = refId.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    var planPart = parts.FirstOrDefault(p => p.StartsWith("P"));
                    var cyclePart = parts.FirstOrDefault(p => p.StartsWith("C"));
                    if (planPart != null && int.TryParse(planPart.Substring(1), out var planId))
                    {
                        var cycle = BillingCycle.Monthly;
                        if (cyclePart != null && int.TryParse(cyclePart.Substring(1), out var c)) cycle = (BillingCycle)c;

                        // Create subscription now that payment succeeded
                        var subscription = await _subscriptionService.SubscribeUserAsync(txn.UserId, planId, cycle, PaymentMethod.Bkash, txn.TransactionReference, txn.Amount, null, txn.GatewayTransactionId);
                    }
                }
                catch { /* ignore parsing errors */ }
            }
            else
            {
                txn.Status = PaymentStatus.Failed;
                await _db.SaveChangesAsync();
            }

            // Show confirmation page
            return RedirectToAction(nameof(Confirmation), new { refId = txn.TransactionReference });
        }

        // ==========================================
        // 1. PRICING MATRIX
        // ==========================================
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var plans = await _subscriptionService.GetAllActivePlansAsync();
            UserSubscription? currentSub = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    currentSub = await _subscriptionService.GetUserActiveSubscriptionAsync(user.Id);
                }
            }

            var vm = new PricingViewModel
            {
                Plans = plans,
                CurrentSubscription = currentSub,
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                CurrentLanguage = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en"
            };

            return View(vm);
        }

        // ==========================================
        // 2. CHECKOUT PAGE
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout(int planId, BillingCycle cycle = BillingCycle.Monthly)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "The selected plan could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // If Free tier, activate immediately without payment
            if (plan.Tier == PlanTier.CitizenFree)
            {
                await _subscriptionService.SubscribeUserAsync(
                    user.Id,
                    plan.Id,
                    BillingCycle.Monthly,
                    PaymentMethod.MockInstant,
                    $"FREE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    0m);

                TempData["SuccessMessage"] = "You have successfully enrolled in the Citizen Free plan!";
                return RedirectToAction(nameof(MySubscription));
            }

            decimal basePrice = cycle == BillingCycle.Yearly ? plan.PriceYearly : plan.PriceMonthly;

            var vm = new CheckoutViewModel
            {
                PlanId = plan.Id,
                Plan = plan,
                Cycle = cycle,
                BasePrice = basePrice,
                DiscountAmount = 0m,
                FinalPrice = basePrice,
                PaymentMethod = PaymentMethod.Bkash,
                CurrentLanguage = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en"
            };

            return View(vm);
        }

        // ==========================================
        // 3. PROCESS CHECKOUT (POST)
        // ==========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var plan = await _subscriptionService.GetPlanByIdAsync(model.PlanId);
            if (plan == null)
            {
                ModelState.AddModelError("", "Selected plan does not exist.");
                return View("Checkout", model);
            }

            var paymentRequest = new PaymentProcessRequest
            {
                UserId = user.Id,
                PlanId = model.PlanId,
                Cycle = model.Cycle,
                Method = model.PaymentMethod,
                Amount = model.FinalPrice,
                MobileNumber = model.MobileNumber,
                PinOrOtp = model.PinOrOtp,
                CardNumber = model.CardNumber,
                CardCvv = model.CardCvv,
                CardExpiry = model.CardExpiry,
                CouponCode = model.CouponCode
            };

            var result = await _paymentService.ProcessSubscriptionPaymentAsync(paymentRequest);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Payment processing failed. Please try again.");
                model.Plan = plan;
                model.BasePrice = model.Cycle == BillingCycle.Yearly ? plan.PriceYearly : plan.PriceMonthly;
                model.FinalPrice = model.BasePrice;
                return View("Checkout", model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Confirmation), new { refId = result.TransactionReference });
        }

        // ==========================================
        // 4. CONFIRMATION
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Confirmation(string refId)
        {
            if (string.IsNullOrWhiteSpace(refId))
            {
                return RedirectToAction(nameof(MySubscription));
            }

            var txn = await _paymentService.GetTransactionByRefAsync(refId);
            if (txn == null)
            {
                return RedirectToAction(nameof(MySubscription));
            }

            ViewBag.CurrentLanguage = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en";
            return View(txn);
        }

        // ==========================================
        // 5. MY SUBSCRIPTION DASHBOARD
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MySubscription()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var activeSub = await _subscriptionService.GetUserActiveSubscriptionAsync(user.Id);
            var plans = await _subscriptionService.GetAllActivePlansAsync();
            var freePlan = plans.FirstOrDefault(p => p.Tier == PlanTier.CitizenFree) ?? new SubscriptionPlan
            {
                Name = "Citizen Free",
                NameBn = "নাগরিক ফ্রি",
                Tier = PlanTier.CitizenFree,
                DailyDenAiQuota = 5,
                MaxFamilyMembers = 1
            };

            var currentPlan = activeSub?.SubscriptionPlan ?? freePlan;

            // Quota
            var (allowed, remaining, limit) = await _subscriptionService.CheckDailyQuotaAsync(user.Id, "DenAi");
            bool isUnlimited = limit == -1 || currentPlan.Tier >= PlanTier.CitizenPremium;
            int usedToday = isUnlimited ? 0 : Math.Max(0, limit - remaining);

            // Family members
            var familyProfiles = await _subscriptionService.GetFamilyProfilesAsync(user.Id);

            // Invoices and Transactions
            var invoices = await _subscriptionService.GetUserInvoicesAsync(user.Id);
            var transactions = await _subscriptionService.GetUserTransactionsAsync(user.Id);

            var vm = new MySubscriptionViewModel
            {
                User = user,
                ActiveSubscription = activeSub,
                CurrentPlan = currentPlan,
                DenAiLimit = limit,
                DenAiRemaining = remaining,
                DenAiUsedToday = usedToday,
                DenAiIsUnlimited = isUnlimited,
                FamilyProfiles = familyProfiles,
                MaxFamilyAllowed = currentPlan.MaxFamilyMembers,
                Invoices = invoices,
                Transactions = transactions,
                CurrentLanguage = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en"
            };

            return View(vm);
        }

        // ==========================================
        // 6. CANCEL SUBSCRIPTION
        // ==========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var success = await _subscriptionService.CancelSubscriptionAsync(user.Id);
            if (success)
            {
                TempData["SuccessMessage"] = "Your subscription auto-renewal has been canceled. Your benefits remain active until the end of your billing cycle.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not find an active subscription to cancel.";
            }

            return RedirectToAction(nameof(MySubscription));
        }

        // ==========================================
        // 7. INVOICE VIEW
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _subscriptionService.GetInvoiceByIdAsync(id, user.Id);
            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Invoice not found or access denied.";
                return RedirectToAction(nameof(MySubscription));
            }

            ViewBag.CurrentLanguage = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en";
            return View(invoice);
        }

        // ==========================================
        // 8. COUPON VALIDATION (AJAX)
        // ==========================================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ValidateCoupon([FromBody] CouponCheckRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Code))
            {
                return Json(new { isValid = false, message = "Please enter a promo coupon code." });
            }

            var result = await _subscriptionService.ValidateCouponAsync(req.Code, req.BaseAmount);
            return Json(new
            {
                isValid = result.IsValid,
                message = result.Message,
                code = result.Code,
                discountPercent = result.DiscountPercent,
                discountAmount = result.DiscountAmount,
                finalAmount = result.FinalAmount
            });
        }

        // ==========================================
        // 9. FAMILY PROFILES MANAGEMENT (AJAX)
        // ==========================================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddFamilyMember([FromBody] FamilyMemberAddRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var activeSub = await _subscriptionService.GetUserActiveSubscriptionAsync(user.Id);
            var maxAllowed = activeSub?.SubscriptionPlan?.MaxFamilyMembers ?? 1;

            var existing = await _subscriptionService.GetFamilyProfilesAsync(user.Id);
            if (existing.Count >= maxAllowed)
            {
                return Json(new
                {
                    success = false,
                    message = $"You have reached your limit of {maxAllowed} profile(s). Upgrade to Citizen Premium to track up to 6 family members."
                });
            }

            var profile = new FamilyProfile
            {
                FullName = req.FullName.Trim(),
                Relationship = req.Relationship,
                Age = req.Age,
                BloodGroup = req.BloodGroup,
                KnownAllergies = req.KnownAllergies,
                MedicalNotes = req.MedicalNotes
            };

            await _subscriptionService.AddFamilyProfileAsync(user.Id, profile);
            return Json(new { success = true, id = profile.Id, message = $"{profile.FullName} added to family health monitoring." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteFamilyMember(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var success = await _subscriptionService.DeleteFamilyProfileAsync(user.Id, id);
            return Json(new { success, message = success ? "Family member removed." : "Could not find member." });
        }
    }
}
