using MediCare.Data;
using MediCare.Models;
using MediCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ---------- Profile ----------

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(new PatientProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PatientProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ---------- Search Doctors ----------

        [HttpGet]
        public async Task<IActionResult> SearchDoctors(string? searchTerm, int? specialtyId)
        {
            var query = _context.Doctors.Include(d => d.Specialty).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(d => d.FullName.Contains(searchTerm));

            if (specialtyId.HasValue)
                query = query.Where(d => d.SpecialtyId == specialtyId.Value);

            var results = await query
                .OrderBy(d => d.FullName)
                .Select(d => new DoctorListItemViewModel
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    Email = d.Email,
                    Phone = d.Phone,
                    ImagePath = d.ImagePath,
                    ExperienceYears = d.ExperienceYears,
                    SpecialtyName = d.Specialty!.Name
                }).ToListAsync();

            var vm = new DoctorSearchViewModel
            {
                SearchTerm = searchTerm,
                SpecialtyId = specialtyId,
                Specialties = await _context.Specialties
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToListAsync(),
                Results = results
            };

            return View(vm);
        }

        // ---------- Book Appointment ----------

        [HttpGet]
        public async Task<IActionResult> BookAppointment(int doctorId)
        {
            var doctor = await _context.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null) return NotFound();

            return View(new BookAppointmentViewModel
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.FullName,
                SpecialtyName = doctor.Specialty?.Name ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            var doctor = await _context.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.Id == model.DoctorId);
            if (doctor == null) return NotFound();

            if (model.AppointmentDate < DateTime.Now)
                ModelState.AddModelError(nameof(model.AppointmentDate), "Appointment date must be in the future.");

            if (!ModelState.IsValid)
            {
                model.DoctorName = doctor.FullName;
                model.SpecialtyName = doctor.Specialty?.Name ?? string.Empty;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            _context.Appointments.Add(new Appointment
            {
                DoctorId = doctor.Id,
                PatientId = user.Id,
                AppointmentDate = model.AppointmentDate,
                Notes = model.Notes,
                Status = AppointmentStatus.Pending
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment booked successfully.";
            return RedirectToAction(nameof(History));
        }

        // ---------- History / Cancel ----------

        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
                .Where(a => a.PatientId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new AppointmentListItemViewModel
                {
                    Id = a.Id,
                    DoctorName = a.Doctor!.FullName,
                    SpecialtyName = a.Doctor.Specialty!.Name,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.PatientId == user.Id);
            if (appointment == null) return NotFound();

            if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            {
                TempData["Error"] = "This appointment can no longer be cancelled.";
                return RedirectToAction(nameof(History));
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment cancelled.";
            return RedirectToAction(nameof(History));
        }
    }
}
