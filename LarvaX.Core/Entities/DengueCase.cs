using System;

namespace LarvaX.Core.Entities
{
    public enum CaseStatus
    {
        Suspected,
        UnderObservation,
        Confirmed,
        Recovering,
        Closed,
        Deceased
    }

    public enum CaseSeverity
    {
        Mild,
        Moderate,
        Severe,
        Critical
    }

    public class DengueCase
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string? PatientAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; } // Male, Female, Other
        public CaseStatus Status { get; set; } = CaseStatus.Suspected;
        public CaseSeverity Severity { get; set; } = CaseSeverity.Mild;
        public int? PlateletCount { get; set; } // e.g., 85000 /uL
        public double? Hematocrit { get; set; } // e.g., 42.5 %
        public string? Symptoms { get; set; }
        public string? FieldNotes { get; set; }
        public string? AssignedWorkerId { get; set; }
        public ApplicationUser? AssignedWorker { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public bool IsEscalated { get; set; } = false;
        public string? EscalationReason { get; set; }
        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
