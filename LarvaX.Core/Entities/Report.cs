namespace LarvaX.Core.Entities
{
    public enum DiseaseType
    {
        Dengue,
        Chikungunya,
        Malaria,
        Zika
    }

    public enum ReportStatus
    {
        Received,
        UnderReview,
        Resolved
    }

    public enum ReportVerification
    {
        Pending,
        Verified,
        Invalid
    }

    public class Report
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? PhotoUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Description { get; set; }
        public DiseaseType DiseaseType { get; set; } = DiseaseType.Dengue;
        public ReportStatus Status { get; set; } = ReportStatus.Received;
        public ReportVerification Verification { get; set; } = ReportVerification.Pending;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
