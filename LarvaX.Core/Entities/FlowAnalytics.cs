namespace LarvaX.Core.Entities
{
    public class FlowAnalytics
    {
        public int Id { get; set; }
        public string FlowName { get; set; } = string.Empty; // e.g., "CitizenReport"
        public string Step { get; set; } = string.Empty; // e.g., "Started", "DetailsFilled", "Completed"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? SessionId { get; set; } // To track a specific user's journey anonymously or specifically
    }
}
