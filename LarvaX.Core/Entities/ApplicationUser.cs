using Microsoft.AspNetCore.Identity;

namespace LarvaX.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? PreferredLanguage { get; set; } = "en"; // en or bn
        public string? ModePreference { get; set; } = "Citizen"; // Citizen or Professional
        
        // Professional roles require admin approval
        public bool IsApproved { get; set; } = false;
        
        public string? RejectionReason { get; set; }

        public string? Specialty { get; set; }
    }
}
