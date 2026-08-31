using LarvaX.Application.Services;
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
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Reply([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest();
            }

            var response = _chatbotService.GetResponse(request.Message, request.Language);

            return Ok(new
            {
                reply = response.Reply,
                isEmergency = response.IsEmergency,
                emergencyMessage = response.EmergencyMessage
            });
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
        public string? Language { get; set; }
    }
}
