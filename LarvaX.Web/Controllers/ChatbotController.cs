using System.Globalization;
using LarvaX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<LarvaX.Core.Entities.ApplicationUser> _userManager;

        public ChatbotController(
            IChatbotService chatbotService,
            ISubscriptionService subscriptionService,
            Microsoft.AspNetCore.Identity.UserManager<LarvaX.Core.Entities.ApplicationUser> userManager)
        {
            _chatbotService = chatbotService;
            _subscriptionService = subscriptionService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? context, string? risk)
        {
            ViewBag.InitialQuery = q;
            ViewBag.Context = context;
            ViewBag.RiskLevel = risk;
            ViewBag.CurrentLang = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en";

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var (allowed, remaining, limit) = await _subscriptionService.CheckDailyQuotaAsync(user.Id, "DenAi");
                    ViewBag.RemainingQuota = remaining;
                    ViewBag.QuotaLimit = limit;
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Reply([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                userId = user?.Id;
            }

            // Check Daily DenAI Quota
            var (allowed, remaining, limit) = await _subscriptionService.CheckDailyQuotaAsync(userId, "DenAi");

            bool isEmergencyQuery = IsEmergencyQuery(request.Message);

            // Gated rule: Emergency triage is NEVER blocked. Only non-emergency general questions are capped on free tier.
            if (!allowed && !isEmergencyQuery)
            {
                bool isBn = request.Language == "bn";
                return Ok(new
                {
                    reply = isBn
                        ? "⚠️ আপনি আপনার দৈনিক ফ্রি ৫টি এআই প্রশ্নের কোটা পূর্ণ করেছেন। জরুরি নির্দেশিকা ব্যতীত আরও আলোচনার জন্য এবং সীমাহীন ২৪/৭ এআই পরামর্শ ও পারিবারিক ট্র্যাকিং পেতে সিটিজেন প্রিমিয়ামে আপগ্রেড করুন।"
                        : "⚠️ You have reached your daily free quota of 5 DenAI consultations. For unlimited 24/7 consultations, family health tracking, and recovery trends, please upgrade to Citizen Premium.",
                    isEmergency = false,
                    emergencyMessage = (string?)null,
                    detectedIntent = "quota_exceeded",
                    actionButtons = new[]
                    {
                        new { text = isBn ? "⭐ প্রিমিয়ামে আপগ্রেড করুন (৳১৯৯/মাস)" : "⭐ Upgrade to Premium (৳199/mo)", url = "/Subscription", isPrimary = true },
                        new { text = isBn ? "জরুরি চিকিৎসা সেবা" : "Emergency First Aid", url = "/FirstAid", isPrimary = false }
                    },
                    suggestedQuestions = Array.Empty<string>(),
                    contextState = request.ContextState,
                    mlConfidence = 1.0,
                    modelSource = "QuotaEnforcement",
                    quotaExceeded = true,
                    remainingQuota = 0
                });
            }

            var response = _chatbotService.GetResponse(request.Message, request.Language, request.ContextState);

            // Record feature usage on allowed non-emergency queries
            await _subscriptionService.RecordFeatureUsageAsync(userId, "DenAi");

            return Ok(new
            {
                reply = response.Reply,
                isEmergency = response.IsEmergency,
                emergencyMessage = response.EmergencyMessage,
                detectedIntent = response.DetectedIntent,
                actionButtons = response.ActionButtons,
                suggestedQuestions = response.SuggestedQuestions,
                contextState = response.ContextState,
                mlConfidence = response.MlConfidence,
                modelSource = response.ModelSource,
                quotaExceeded = false,
                remainingQuota = limit == -1 ? 999999 : Math.Max(0, remaining - 1)
            });
        }

        private static bool IsEmergencyQuery(string message)
        {
            var lower = message.ToLowerInvariant();
            return lower.Contains("emergency") ||
                   lower.Contains("shock") ||
                   lower.Contains("bleed") ||
                   lower.Contains("blood") ||
                   lower.Contains("unconscious") ||
                   lower.Contains("convulsion") ||
                   lower.Contains("faint") ||
                   lower.Contains("severe") ||
                   lower.Contains("999") ||
                   lower.Contains("জরুরি") ||
                   lower.Contains("রক্ত") ||
                   lower.Contains("শক") ||
                   lower.Contains("অচেতন") ||
                   lower.Contains("খিঁচুনি");
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
        public string? Language { get; set; }
        public string? ContextState { get; set; }
    }
}
