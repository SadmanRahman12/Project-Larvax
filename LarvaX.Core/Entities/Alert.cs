namespace LarvaX.Core.Entities
{
    public class Alert
    {
        public int Id { get; set; }
        public string Message { get; set; } = null!;
        public string Language { get; set; } = "en";
        public int? ZoneId { get; set; }
        public RiskZone? Zone { get; set; }
        public double? RadiusKm { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public int DeliveredCount { get; set; }
        public int OpenedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
