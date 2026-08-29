using System.ComponentModel.DataAnnotations;
using LarvaX.Core.Entities;

namespace LarvaX.Web.Models
{
    public class DonorRegisterViewModel
    {
        [Required]
        [Display(Name = "Blood Group")]
        public BloodGroup BloodGroup { get; set; }

        [Required]
        [Display(Name = "District / Area")]
        public string Location { get; set; } = null!;

        [Required]
        [Display(Name = "Contact Number")]
        [Phone]
        public string ContactNumber { get; set; } = null!;

        [Display(Name = "Latitude")]
        public double Latitude { get; set; } = 23.8103;

        [Display(Name = "Longitude")]
        public double Longitude { get; set; } = 90.4125;
    }
}
