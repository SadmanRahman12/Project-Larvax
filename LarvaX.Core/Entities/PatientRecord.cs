using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LarvaX.Core.Entities
{
    public class PatientRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [ForeignKey(nameof(PatientId))]
        public ApplicationUser? Patient { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? FileUrl { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string RecordType { get; set; } = "Prescription"; // Prescription, LabResult, MedicalHistory
    }
}
