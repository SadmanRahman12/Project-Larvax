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
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, SampleCollected, SampleReceived, Processing, Completed, Rejected, Cancelled

        [StringLength(50)]
        public string SampleStatus { get; set; } = "Pending"; // Pending, Collected, Received, Processing, Completed, Rejected

        [StringLength(50)]
        public string? SampleType { get; set; } = "Venous Blood (EDTA)";

        [StringLength(50)]
        public string? BarcodeNumber { get; set; }

        public DateTime? SampleCollectedAt { get; set; }
        public DateTime? SampleReceivedAt { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [StringLength(300)]
        public string? RejectionReason { get; set; }

        // Structured Dengue & Lab Test Values
        public int? PlateletCount { get; set; }
        public int? WbcCount { get; set; }
        public double? Hematocrit { get; set; }

        [StringLength(30)]
        public string? DengueNs1 { get; set; } // Negative, Positive, Equivocal

        [StringLength(30)]
        public string? DengueIgm { get; set; } // Negative, Positive, Equivocal

        [StringLength(30)]
        public string? DengueIgg { get; set; } // Negative, Positive, Equivocal

        [StringLength(1000)]
        public string? TestNotes { get; set; }

        [StringLength(1000)]
        public string? ResultLink { get; set; }

        public bool IsVerified { get; set; } = false;

        [StringLength(100)]
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public bool PatientNotified { get; set; } = false;

        [StringLength(450)]
        public string? LabStaffId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
