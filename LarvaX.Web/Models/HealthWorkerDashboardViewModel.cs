using System;
using System.Collections.Generic;
using System.Linq;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class HealthWorkerDashboardViewModel
    {
        // Logged-in health worker
        public ApplicationUser Worker { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();

        // ── 1. Overview Summary Cards ─────────────────────────────────────────
        public int HighRiskCasesCount { get; set; }
        public int PendingReportsCount { get; set; }
        public int ActiveAlertsCount { get; set; }
        public int LowStockItemsCount { get; set; }
        public int CasesRequiringFollowUpCount { get; set; }
        public int AssignedTasksCount { get; set; }

        // ── 2. Dengue Case Management ─────────────────────────────────────────
        public List<DengueCase> DengueCases { get; set; } = new();
        public int SuspectedCasesCount => DengueCases.Count(c => c.Status == CaseStatus.Suspected);
        public int UnderObservationCasesCount => DengueCases.Count(c => c.Status == CaseStatus.UnderObservation);
        public int ConfirmedCasesCount => DengueCases.Count(c => c.Status == CaseStatus.Confirmed);
        public int RecoveringCasesCount => DengueCases.Count(c => c.Status == CaseStatus.Recovering);
        public int ClosedCasesCount => DengueCases.Count(c => c.Status == CaseStatus.Closed);
        public int EscalatedCasesCount => DengueCases.Count(c => c.IsEscalated);

        // ── 3. Citizen Report Queue ───────────────────────────────────────────
        public List<Report> CitizenReports { get; set; } = new();
        public int NewReportsCount => CitizenReports.Count(r => r.Status == ReportStatus.Received);
        public int UnderReviewReportsCount => CitizenReports.Count(r => r.Status == ReportStatus.UnderReview);
        public int InvestigatingReportsCount => CitizenReports.Count(r => r.Status == ReportStatus.Investigating || r.Status == ReportStatus.Assigned);
        public int ResolvedReportsCount => CitizenReports.Count(r => r.Status == ReportStatus.Resolved);
        public int EscalatedReportsCount => CitizenReports.Count(r => r.Status == ReportStatus.Escalated);

        // ── 4. Dengue Risk Map ─────────────────────────────────────────────────
        public List<RiskZone> RiskZones { get; set; } = new();
        public int HighRiskZonesCount => RiskZones.Count(z => z.RiskLevel == RiskLevel.High);

        // ── 5. Alerts & Notifications ─────────────────────────────────────────
        public List<Alert> RecentAlerts { get; set; } = new();

        // ── 6. Inventory Management ───────────────────────────────────────────
        public List<InventoryItem> InventoryItems { get; set; } = new();
        public List<InventoryItem> LowStockItems => InventoryItems.Where(i => i.Quantity <= i.Threshold).ToList();
        public List<InventoryTransaction> RecentTransactions { get; set; } = new();

        // ── 7. Patient Records ────────────────────────────────────────────────
        public List<PatientRecord> PatientRecords { get; set; } = new();

        // ── 8. Referral & Escalation ──────────────────────────────────────────
        public List<CaseReferral> Referrals { get; set; } = new();
        public int PendingReferralsCount => Referrals.Count(r => r.Status == ReferralStatus.Pending);

        // ── 9. Blood Donor / Resource Search ──────────────────────────────────
        public List<Donor> AvailableDonors { get; set; } = new();
        public Dictionary<int, string> DonorFreshnessLabels { get; set; } = new();
        public Dictionary<int, bool> DonorFreshnessFlags { get; set; } = new();

        // ── 10. Health Education & Awareness ──────────────────────────────────
        public List<Article> EducationArticles { get; set; } = new();
        public List<Quiz> Quizzes { get; set; } = new();

        // ── 11. Task / Field Activity Management ──────────────────────────────
        public List<HealthWorkerTask> Tasks { get; set; } = new();
        public int PendingTasksCount => Tasks.Count(t => !t.IsCompleted);
        public int CompletedTasksCount => Tasks.Count(t => t.IsCompleted);

        // ── 13. Language & Preferences ────────────────────────────────────────
        public string CurrentLanguage { get; set; } = "en";
    }

    public class AddDengueCaseInputModel
    {
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string? PatientAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = "Male";
        public CaseStatus Status { get; set; } = CaseStatus.Suspected;
        public CaseSeverity Severity { get; set; } = CaseSeverity.Mild;
        public int? PlateletCount { get; set; }
        public double? Hematocrit { get; set; }
        public string? Symptoms { get; set; }
        public string? FieldNotes { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public bool IsEscalated { get; set; }
        public string? EscalationReason { get; set; }
    }

    public class UpdateCaseStatusInputModel
    {
        public int CaseId { get; set; }
        public CaseStatus Status { get; set; }
        public CaseSeverity Severity { get; set; }
        public int? PlateletCount { get; set; }
        public double? Hematocrit { get; set; }
        public string? FollowUpNotes { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public bool IsEscalated { get; set; }
        public string? EscalationReason { get; set; }
    }

    public class UpdateCitizenReportInputModel
    {
        public int ReportId { get; set; }
        public ReportStatus Status { get; set; }
        public ReportVerification Verification { get; set; }
        public string? FieldNotes { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class InventoryAdjustmentInputModel
    {
        public int ItemId { get; set; }
        public int QuantityChange { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateReferralInputModel
    {
        public int? DengueCaseId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public ReferralTarget Target { get; set; } = ReferralTarget.Doctor;
        public ReferralUrgency Urgency { get; set; } = ReferralUrgency.Urgent;
        public string Reason { get; set; } = string.Empty;
        public string? ClinicalNotes { get; set; }
    }

    public class HealthWorkerTaskInputModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "ReportInvestigation";
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? DueDate { get; set; }
    }

    public class BroadcastAlertInputModel
    {
        public string Message { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public int? ZoneId { get; set; }
        public double? RadiusKm { get; set; } = 3.0;
    }
}
