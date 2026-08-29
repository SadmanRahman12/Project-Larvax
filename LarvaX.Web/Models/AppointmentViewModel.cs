using System;

namespace LarvaX.Web.Models
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? ScheduledAt { get; set; } // yyyy-MM-ddTHH:mm from datetime-local
        public string? Notes { get; set; }
    }
}
