using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediCare.ViewModels
{
    // Used for both Create and Edit forms in Admin > Manage Doctors
    public class DoctorFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Range(0, 60)]
        [Display(Name = "Experience (years)")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Please select a specialty.")]
        [Display(Name = "Specialty")]
        public int SpecialtyId { get; set; }

        [Display(Name = "Photo")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        public IEnumerable<SelectListItem> Specialties { get; set; } = new List<SelectListItem>();
    }

    // Read-only card used in listings / search results
    public class DoctorListItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public int ExperienceYears { get; set; }
        public string SpecialtyName { get; set; } = string.Empty;
    }

    public class DoctorSearchViewModel
    {
        [Display(Name = "Search by name")]
        public string? SearchTerm { get; set; }

        public int? SpecialtyId { get; set; }

        public IEnumerable<SelectListItem> Specialties { get; set; } = new List<SelectListItem>();

        public List<DoctorListItemViewModel> Results { get; set; } = new();
    }
}
