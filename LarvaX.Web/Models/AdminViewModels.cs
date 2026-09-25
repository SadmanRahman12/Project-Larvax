using System;
using System.Collections.Generic;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class AdminDashboardOverviewViewModel
    {
        public int TotalUsersCount { get; set; }
        public int PendingApprovalsCount { get; set; }
        public int PendingReportsCount { get; set; }
        public int VerifiedReportsCount { get; set; }
        public int RejectedReportsCount { get; set; }
        public int ActiveAlertsCount { get; set; }
        public int HighRiskZonesCount { get; set; }
        public int TotalDengueCasesCount { get; set; }

        public List<ApplicationUser> PendingApprovals { get; set; } = new();
        public List<Report> PendingReports { get; set; } = new();
        public List<Alert> RecentAlerts { get; set; } = new();
        public List<AdminActivityItem> RecentActivities { get; set; } = new();

        public string MapZonesJson { get; set; } = "[]";
        public string MapReportsJson { get; set; } = "[]";
        public string MapCasesJson { get; set; } = "[]";

        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public Dictionary<string, int> ReportsByStatus { get; set; } = new();
        public Dictionary<string, double> AbandonmentRates { get; set; } = new();
    }

    public class AdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? ModePreference { get; set; }
        public string? Specialty { get; set; }
        public string? Address { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
        public bool EmailConfirmed { get; set; }
    }

    public class AdminUserManagementViewModel
    {
        public List<AdminUserListItemViewModel> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? SelectedRole { get; set; }
        public string? SelectedStatus { get; set; }
        public Dictionary<string, int> RoleCounts { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
    }

    public class AdminRiskMonitoringViewModel
    {
        public List<RiskZone> RiskZones { get; set; } = new();
        public int HighRiskZonesCount { get; set; }
        public int MediumRiskZonesCount { get; set; }
        public int LowRiskZonesCount { get; set; }
        public int SufficientCount { get; set; }
        public int PartialCount { get; set; }
        public int InsufficientCount { get; set; }
        public DateTime? LastModelRun { get; set; }

        public string MapZonesJson { get; set; } = "[]";
        public string MapReportsJson { get; set; } = "[]";
        public string MapCasesJson { get; set; } = "[]";
    }

    public class AdminAlertManagementViewModel
    {
        public List<Alert> Alerts { get; set; } = new();
        public List<RiskZone> RiskZones { get; set; } = new();
        public int TotalAlertsCount => Alerts.Count;
        public int TotalDeliveredCount { get; set; }
        public int TotalOpenedCount { get; set; }
        public int TotalFailedCount { get; set; }
        public int ActiveTodayCount { get; set; }
    }

    public class BroadcastAlertFormModel
    {
        public string Message { get; set; } = string.Empty;
        public string? MessageBn { get; set; }
        public int? ZoneId { get; set; }
        public double RadiusKm { get; set; } = 5.0;
        public string Language { get; set; } = "en";
    }

    public class AdminAnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalReports { get; set; }
        public int TotalCases { get; set; }
        public int TotalAlerts { get; set; }

        public string UserRoleLabelsJson { get; set; } = "[]";
        public string UserRoleValuesJson { get; set; } = "[]";

        public string DiseaseLabelsJson { get; set; } = "[]";
        public string DiseaseValuesJson { get; set; } = "[]";

        public string ReportStatusLabelsJson { get; set; } = "[]";
        public string ReportStatusValuesJson { get; set; } = "[]";

        public string DailyReportTrendLabelsJson { get; set; } = "[]";
        public string DailyReportTrendValuesJson { get; set; } = "[]";

        public Dictionary<string, Dictionary<string, int>> FlowStepCounts { get; set; } = new();
        public Dictionary<string, double> FlowAbandonmentRates { get; set; } = new();
    }

    public class AdminActivityItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = "System"; // Users, Reports, Risk, Alerts, Settings
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Actor { get; set; } = "System";
        public string BadgeClass { get; set; } = "bg-primary";
        public string Icon { get; set; } = "bi-info-circle";
    }

    public class AdminActivityLogViewModel
    {
        public List<AdminActivityItem> Activities { get; set; } = new();
        public string? FilterCategory { get; set; }
        public string? FilterSearch { get; set; }
    }

    public class AdminDataReportsViewModel
    {
        public int TotalReports { get; set; }
        public int TotalVerifiedReports { get; set; }
        public int TotalPendingReports { get; set; }
        public int TotalInvalidReports { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAlerts { get; set; }
        public int TotalCases { get; set; }

        public List<Report> RecentReports { get; set; } = new();
        public List<Alert> RecentAlerts { get; set; } = new();
    }

    public class AdminSettingsViewModel
    {
        public bool RequireProfessionalApproval { get; set; } = true;
        public double DefaultAlertRadiusKm { get; set; } = 5.0;
        public bool EnableSignalRBroadcast { get; set; } = true;
        public int CriticalPlateletThreshold { get; set; } = 50000;
        public bool MaintenanceMode { get; set; } = false;
        public string DefaultLanguage { get; set; } = "en";
        public bool AutoEscalateSevereReports { get; set; } = true;
    }
}
