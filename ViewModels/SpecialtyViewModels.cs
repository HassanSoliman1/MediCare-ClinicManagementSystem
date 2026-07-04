using System.ComponentModel.DataAnnotations;

namespace MediCare.ViewModels
{
    public class SpecialtyFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DoctorCount { get; set; }
    }
}
