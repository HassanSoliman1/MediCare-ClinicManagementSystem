using System.ComponentModel.DataAnnotations;

namespace MediCare.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TotalSpecialties { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public List<AppointmentListItemViewModel> RecentAppointments { get; set; } = new();
    }

    public class PatientListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int TotalAppointments { get; set; }
    }

    public class PatientProfileViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
    }

    public class PatientDetailsViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<AppointmentListItemViewModel> AppointmentHistory { get; set; } = new();
    }
}
