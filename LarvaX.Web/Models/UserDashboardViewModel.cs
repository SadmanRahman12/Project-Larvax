using System.Collections.Generic;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class UserDashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();

        // Metric Totals
        public int TotalReportsCount { get; set; }
        public int TotalAppointmentsCount { get; set; }
        public int TotalLabBookingsCount { get; set; }
        public int TotalRecordsCount { get; set; }

        // Recent Activity Lists
        public List<Report> RecentReports { get; set; } = new();
        public List<Appointment> UpcomingAppointments { get; set; } = new();
        public List<LabBooking> RecentLabBookings { get; set; } = new();
        public List<PatientRecord> RecentRecords { get; set; } = new();

        // Blood Donor Status
        public Donor? DonorProfile { get; set; }

        // Surveillance Overview
        public int ActiveHighRiskZonesCount { get; set; }
        public int TotalActiveAlertsCount { get; set; }
    }
}
