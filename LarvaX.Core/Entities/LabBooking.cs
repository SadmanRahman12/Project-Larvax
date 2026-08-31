using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LarvaX.Core.Entities
{
    public class LabBooking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [ForeignKey(nameof(PatientId))]
        public ApplicationUser? Patient { get; set; }

        [Required]
        public int LabTestId { get; set; }

        [ForeignKey(nameof(LabTestId))]
        public LabTest? LabTest { get; set; }

        public DateTime ScheduledAt { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, SampleCollected, Completed

        [StringLength(1000)]
        public string? ResultLink { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
