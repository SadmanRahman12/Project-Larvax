using System.ComponentModel.DataAnnotations;

namespace LarvaX.Web.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;

        [Required]
        public string PreferredLanguage { get; set; } = "en";

        public string? Specialty { get; set; }
    }
}
