using System;

namespace LarvaX.Core.Entities
{
    public class FamilyProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public string Relationship { get; set; } = "Spouse"; // Child, Spouse, Parent, Sibling, Other
        public int? Age { get; set; }
        public string? BloodGroup { get; set; }
        public string? KnownAllergies { get; set; }
        public string? MedicalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
