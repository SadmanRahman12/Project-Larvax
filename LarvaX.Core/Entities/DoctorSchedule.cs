using System;

namespace LarvaX.Core.Entities
{
    public class DoctorSchedule
    {
        public int Id { get; set; }

        public string DoctorId { get; set; } = null!;
        public ApplicationUser Doctor { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; } // Sunday = 0, Monday = 1, ... Saturday = 6

        public TimeSpan StartTime { get; set; } // e.g. 09:00:00

        public TimeSpan EndTime { get; set; } // e.g. 13:00:00

        public int SlotDurationMinutes { get; set; } = 30; // e.g. 15, 20, 30, 45, 60 minutes

        public string? ShiftName { get; set; } // e.g. "Morning Consultation", "Evening Clinic"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
