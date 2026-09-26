using System.Globalization;
using LarvaX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        public IActionResult Index(string? q, string? context, string? risk)
        {
            ViewBag.InitialQuery = q;
            ViewBag.Context = context;
            ViewBag.RiskLevel = risk;
            ViewBag.CurrentLang = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en";
            return View();
        }

        [HttpPost]
        public IActionResult Reply([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var response = _chatbotService.GetResponse(request.Message, request.Language, request.ContextState);

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
                modelSource = response.ModelSource
            });
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
        public string? Language { get; set; }
        public string? ContextState { get; set; }
    }
}
