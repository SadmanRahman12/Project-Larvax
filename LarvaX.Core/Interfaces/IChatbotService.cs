namespace LarvaX.Core.Interfaces
{
    /// <summary>
    /// Bilingual keyword-based dengue health chatbot.
    /// Interface lives in Core so it can be referenced across all layers.
    /// </summary>
    public interface IChatbotService
    {
        ChatbotResponse GetResponse(string message, string? language);
    }

    /// <summary>
    /// Returned by <see cref="IChatbotService.GetResponse"/>.
    /// Defined here (not in Application) so callers in Web can reference the type without coupling to Application.
    /// </summary>
    public class ChatbotResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public string? EmergencyMessage { get; set; }
    }
}
