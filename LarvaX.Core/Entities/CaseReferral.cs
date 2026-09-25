using System;

namespace LarvaX.Core.Entities
{
    public enum ReferralTarget
    {
        Doctor,
        Hospital,
        IcuFacility,
        Laboratory,
        Specialist
    }

    public enum ReferralUrgency
    {
        Routine,
        Urgent,
        Emergency
    }

    public enum ReferralStatus
    {
        Pending,
        Accepted,
        Completed,
        Cancelled
    }

    public class CaseReferral
    {
        public int Id { get; set; }
        public int? DengueCaseId { get; set; }
        public DengueCase? DengueCase { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public ReferralTarget Target { get; set; } = ReferralTarget.Doctor;
        public ReferralUrgency Urgency { get; set; } = ReferralUrgency.Urgent;
        public ReferralStatus Status { get; set; } = ReferralStatus.Pending;
        public string Reason { get; set; } = string.Empty;
        public string? ClinicalNotes { get; set; }
        public string? ReferredById { get; set; }
        public ApplicationUser? ReferredBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
