using System;

namespace LarvaX.Core.Entities
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public class HealthWorkerTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "General"; // ReportInvestigation, AreaVisit, PatientFollowUp, InventoryCheck, ReferralCoordination, CommunityAwareness
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public bool IsCompleted { get; set; } = false;
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? HealthWorkerId { get; set; }
        public ApplicationUser? HealthWorker { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
