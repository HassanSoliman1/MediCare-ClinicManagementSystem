using System.ComponentModel.DataAnnotations;
using MediCare.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediCare.ViewModels
{
    public class BookAppointmentViewModel
    {
        public int DoctorId { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string SpecialtyName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Appointment Date & Time")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime AppointmentDate { get; set; } = DateTime.Now.AddDays(1);

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class AppointmentListItemViewModel
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateAppointmentStatusViewModel
    {
        public int Id { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    public class AdminAppointmentFilterViewModel
    {
        public string? StatusFilter { get; set; }
        public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<AppointmentListItemViewModel> Appointments { get; set; } = new();
    }
}
