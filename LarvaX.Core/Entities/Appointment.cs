using System;

namespace LarvaX.Core.Entities
{
    public enum AppointmentStatus
    {
        Booked,
        Confirmed,
        Cancelled,
        Completed
    }

    public class Appointment
    {
        public int Id { get; set; }

        // Patient
        public string PatientId { get; set; } = null!;
        public ApplicationUser Patient { get; set; } = null!;

        // Doctor
        public string DoctorId { get; set; } = null!;
        public ApplicationUser Doctor { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

        // Video consult room identifier used by signaling hub
        public string? VideoRoomId { get; set; }

        public string? Notes { get; set; }

        public bool FollowUpRequired { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
