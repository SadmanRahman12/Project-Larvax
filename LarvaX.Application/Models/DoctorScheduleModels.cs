using System;
using System.Collections.Generic;

namespace LarvaX.Application.Models
{
    public class DoctorAvailableSlotDto
    {
        public DateTime SlotDateTime { get; set; }
        public string TimeFormatted { get; set; } = string.Empty; // e.g. "09:00 AM"
        public string SlotValue { get; set; } = string.Empty;     // e.g. "2026-09-26T09:00"
        public bool IsAvailable { get; set; }
        public string? ShiftName { get; set; }
        public string? UnavailabilityReason { get; set; }
    }

    public class DoctorScheduleInputModel
    {
        public int? Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
        public string StartTime { get; set; } = "09:00"; // HH:mm
        public string EndTime { get; set; } = "13:00";   // HH:mm
        public int SlotDurationMinutes { get; set; } = 30;
        public string? ShiftName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DoctorScheduleSummaryDto
    {
        public string DoctorId { get; set; } = null!;
        public bool HasSchedule { get; set; }
        public string SummaryText { get; set; } = string.Empty;
        public List<string> AvailableDays { get; set; } = new();
    }
}
