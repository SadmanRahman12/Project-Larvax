using System.Collections.Generic;

namespace LarvaX.Core.Interfaces
{
    /// <summary>
    /// Bilingual dengue health assistant chatbot service (DenAI).
    /// Interface lives in Core so it can be referenced across all layers.
    /// </summary>
    public interface IChatbotService
    {
        ChatbotResponse GetResponse(string message, string? language);
        ChatbotResponse GetResponse(string message, string? language, string? contextState);
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
        public string DetectedIntent { get; set; } = "GeneralInfo";
        public List<ChatActionButton> ActionButtons { get; set; } = new();
        public List<string> SuggestedQuestions { get; set; } = new();
        public string? ContextState { get; set; }
        /// <summary>ML model confidence score for the detected intent (0.0 – 1.0).</summary>
        public float MlConfidence { get; set; }
        /// <summary>"ML" when the response was routed by the ML.NET classifier, "Safety" for emergency overrides.</summary>
        public string ModelSource { get; set; } = "ML";
    }


    /// <summary>
    /// Represents an actionable service route button returned by DenAI.
    /// </summary>
    public class ChatActionButton
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-arrow-right-circle";
        public string ButtonClass { get; set; } = "btn-outline-primary";
    }
}
