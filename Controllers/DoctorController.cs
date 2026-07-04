using MediCare.Data;
using MediCare.Models;
using MediCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Models.Doctor?> GetCurrentDoctorAsync()
        {
            var userId = _userManager.GetUserId(User);
            return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<IActionResult> Appointments()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                TempData["Error"] = "No doctor profile is linked to this account. Please contact the administrator.";
                return View(new List<AppointmentListItemViewModel>());
            }

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new AppointmentListItemViewModel
                {
                    Id = a.Id,
                    DoctorName = doctor.FullName,
                    PatientId = a.PatientId,
                    PatientName = a.Patient!.FullName,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToListAsync();

            return View(appointments);
        }

        public async Task<IActionResult> PatientDetails(string id)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return Forbid();

            // Ensure this doctor has at least one appointment with the patient
            var hasRelation = await _context.Appointments.AnyAsync(a => a.DoctorId == doctor.Id && a.PatientId == id);
            if (!hasRelation) return Forbid();

            var patient = await _userManager.FindByIdAsync(id);
            if (patient == null) return NotFound();

            var history = await _context.Appointments
                .Where(a => a.PatientId == id && a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new AppointmentListItemViewModel
                {
                    Id = a.Id,
                    DoctorName = doctor.FullName,
                    PatientName = patient.FullName,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToListAsync();

            var vm = new PatientDetailsViewModel
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                Email = patient.Email ?? string.Empty,
                PhoneNumber = patient.PhoneNumber,
                AppointmentHistory = history
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return Forbid();

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);
            if (appointment == null) return NotFound();

            return View(new UpdateAppointmentStatusViewModel
            {
                Id = appointment.Id,
                Status = appointment.Status,
                Notes = appointment.Notes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(UpdateAppointmentStatusViewModel model)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return Forbid();

            if (!ModelState.IsValid) return View(model);

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == model.Id && a.DoctorId == doctor.Id);
            if (appointment == null) return NotFound();

            appointment.Status = model.Status;
            appointment.Notes = model.Notes;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment status updated.";
            return RedirectToAction(nameof(Appointments));
        }
    }
}
