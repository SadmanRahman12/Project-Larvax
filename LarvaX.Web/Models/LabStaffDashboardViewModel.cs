using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class LabStaffDashboardViewModel
    {
        // Logged-in staff
        public ApplicationUser LabStaff { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();

        // ── Overview KPI Stats ──────────────────────────────────────────────────
        public int TodayTestsCount { get; set; }
        public int PendingTestsCount { get; set; }
        public int ResultsWaitingUploadCount { get; set; }
        public int CompletedTestsCount { get; set; }
        public int TotalBookingsCount { get; set; }
        public int CriticalPlateletAlertsCount { get; set; }

        // ── Bookings & Workflow Lists ───────────────────────────────────────────
        public List<LabBooking> TodayBookings { get; set; } = new();
        public List<LabBooking> PendingTests { get; set; } = new();
        public List<LabBooking> WaitingUploadTests { get; set; } = new();
        public List<LabBooking> CompletedTests { get; set; } = new();
        public List<LabBooking> AllBookings { get; set; } = new();

        // ── Available Tests Management ──────────────────────────────────────────
        public List<LabTest> AvailableTests { get; set; } = new();

        // ── Patients & Records Integration ──────────────────────────────────────
        public List<LabPatientSummary> Patients { get; set; } = new();
        public List<PatientRecord> RecentLabPatientRecords { get; set; } = new();

        // ── Lab Activity Feed & Alerts ──────────────────────────────────────────
        public List<LabActivityItem> RecentActivities { get; set; } = new();
        public List<Alert> UrgentAlerts { get; set; } = new();

        // ── Laboratory Profile Info ─────────────────────────────────────────────
        public LabProfileInfo LabProfile { get; set; } = new();

        // Active tab / search
        public string? SearchTerm { get; set; }
        public string? ActiveTab { get; set; } = "overview";
    }

    public class LabPatientSummary
    {
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int TotalTests { get; set; }
        public DateTime? LatestTestDate { get; set; }
        public string? LatestTestName { get; set; }
        public string? LatestStatus { get; set; }
        public bool HasCriticalResult { get; set; }
    }

    public class LabActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-activity";
        public string BadgeClass { get; set; } = "bg-primary";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class LabProfileInfo
    {
        public string Name { get; set; } = "LarvaX Dengue Diagnostic Reference Laboratory";
        public string FacilityCode { get; set; } = "LX-LAB-DHAKA-01";
        public string Address { get; set; } = "IEDCR Dengue Surveillance Annex, Mohakhali Health Complex, Dhaka-1212";
        public string ContactPhone { get; set; } = "+880 2-9898796 / +880 1711-000222";
        public string ContactEmail { get; set; } = "lab-surveillance@larvax.gov.bd";
        public string OperatingHours { get; set; } = "24/7 Emergency Dengue Screening & Diagnostics";
        public string Accreditation { get; set; } = "DGHS Clinical Pathology Certified • ISO 15189 Quality Compliant";
        public string LaboratoryDirector { get; set; } = "Dr. M. S. Rahman, MBBS, MD (Pathology)";
    }

    public class UpdateSampleStatusInputModel
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty; // SampleCollected, SampleReceived, Processing, Completed, Rejected
        public string? RejectionReason { get; set; }
        public string? BarcodeNumber { get; set; }
    }

    public class UploadLabResultInputModel
    {
        public int BookingId { get; set; }
        public int? PlateletCount { get; set; }
        public int? WbcCount { get; set; }
        public double? Hematocrit { get; set; }
        public string? DengueNs1 { get; set; }
        public string? DengueIgm { get; set; }
        public string? DengueIgg { get; set; }
        public string? TestNotes { get; set; }
        public IFormFile? ResultFile { get; set; }
        public string? ExternalFileUrl { get; set; }
        public bool AutoVerify { get; set; } = true;
    }

    public class LabTestManageInputModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
