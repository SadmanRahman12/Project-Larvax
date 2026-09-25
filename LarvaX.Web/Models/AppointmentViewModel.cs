using System;
using System.Collections.Generic;
using LarvaX.Application.Models;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorSpecialty { get; set; }
        public string? DoctorEmail { get; set; }
        public string? ScheduledAt { get; set; } // yyyy-MM-ddTHH:mm from datetime slot
        public string? Notes { get; set; }

        // Scheduling support
        public bool HasSchedule { get; set; }
        public string SelectedDate { get; set; } = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        public List<DoctorSchedule> Schedules { get; set; } = new();
        public List<DoctorAvailableSlotDto> AvailableSlots { get; set; } = new();
        public List<string> WorkingDaysSummary { get; set; } = new();
    }
}
