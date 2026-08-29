using System.ComponentModel.DataAnnotations;
using LarvaX.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace LarvaX.Web.Models
{
    public class ReportCreateViewModel
    {
        [Required]
        [Display(Name = "Latitude")]
        public double Latitude { get; set; } = 23.8103; // Default to Dhaka

        [Required]
        [Display(Name = "Longitude")]
        public double Longitude { get; set; } = 90.4125;

        [Display(Name = "Description of Hazard / Suspected Case")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Disease Type")]
        public DiseaseType DiseaseType { get; set; } = DiseaseType.Dengue;

        [Display(Name = "Site Photo")]
        public IFormFile? Photo { get; set; }
    }
}
