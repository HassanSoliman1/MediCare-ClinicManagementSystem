using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        // Stored file name of the uploaded photo (wwwroot/images/doctors/{ImagePath})
        public string? ImagePath { get; set; }

        [Range(0, 60)]
        public int ExperienceYears { get; set; }

        [Required]
        public int SpecialtyId { get; set; }

        [ForeignKey(nameof(SpecialtyId))]
        public Specialty? Specialty { get; set; }

        // Linked Identity account so the doctor can log in.
        // Nullable because an admin may create a doctor profile before
        // an Identity account is generated for them.
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
