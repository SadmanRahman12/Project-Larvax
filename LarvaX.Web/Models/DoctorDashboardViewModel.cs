using System;
using System.Collections.Generic;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class DoctorDashboardViewModel
    {
        // Logged-in doctor
        public ApplicationUser Doctor { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();

        // ── Overview Stats ─────────────────────────────────────────────────────
        public int TodayAppointmentsCount { get; set; }
        public int PendingRequestsCount { get; set; }
        public int ActiveConsultationsCount { get; set; }
        public int TotalPatientsCount { get; set; }
        public int HighRiskPatientCount { get; set; }

        // ── Appointments ───────────────────────────────────────────────────────
        /// <summary>Today's appointments (as doctor) in chronological order.</summary>
        public List<Appointment> TodayAppointments { get; set; } = new();

        /// <summary>All upcoming appointments for the doctor.</summary>
        public List<Appointment> UpcomingAppointments { get; set; } = new();

        /// <summary>Consultation requests pending doctor confirmation.</summary>
        public List<Appointment> PendingRequests { get; set; } = new();

        // ── Patient records the doctor has seen ───────────────────────────────
        public List<PatientRecord> RecentPatientRecords { get; set; } = new();

        // ── Lab bookings doctor needs to review ───────────────────────────────
        public List<LabBooking> RecentLabResults { get; set; } = new();

        // ── Dengue Risk / Alerts ──────────────────────────────────────────────
        public int ActiveHighRiskZonesCount { get; set; }
        public List<RiskZone> HighRiskZones { get; set; } = new();
        public List<Alert> RecentAlerts { get; set; } = new();

        // ── Doctor Schedule & Working Hours ────────────────────────────────────
        public List<DoctorSchedule> Schedules { get; set; } = new();
        public int ActiveSchedulesCount => Schedules.Count(s => s.IsActive);
        public List<string> ScheduledDays => Schedules.Where(s => s.IsActive).Select(s => s.DayOfWeek.ToString().Substring(0, 3)).Distinct().ToList();
    }
}
