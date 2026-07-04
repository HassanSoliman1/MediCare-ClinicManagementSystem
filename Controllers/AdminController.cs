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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ---------- Dashboard ----------

        public async Task<IActionResult> Dashboard()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
                .Include(a => a.Patient)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var patientIds = await _userManager.GetUsersInRoleAsync("Patient");

            var vm = new AdminDashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalPatients = patientIds.Count,
                TotalSpecialties = await _context.Specialties.CountAsync(),
                TotalAppointments = appointments.Count,
                PendingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Pending),
                ConfirmedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Confirmed),
                CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                RecentAppointments = appointments.Take(5).Select(a => new AppointmentListItemViewModel
                {
                    Id = a.Id,
                    DoctorName = a.Doctor?.FullName ?? "-",
                    SpecialtyName = a.Doctor?.Specialty?.Name ?? "-",
                    PatientName = a.Patient?.FullName ?? "-",
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToList()
            };

            return View(vm);
        }

        // ---------- Doctors CRUD ----------

        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Specialty)
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

            return View(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> CreateDoctor()
        {
            var vm = new DoctorFormViewModel { Specialties = await GetSpecialtySelectListAsync() };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoctor(DoctorFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Specialties = await GetSpecialtySelectListAsync();
                return View(model);
            }

            var doctor = new Doctor
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                ExperienceYears = model.ExperienceYears,
                SpecialtyId = model.SpecialtyId
            };

            if (model.ImageFile != null)
                doctor.ImagePath = await SaveDoctorImageAsync(model.ImageFile);

            // Create a linked Identity login for the doctor, defaulting password;
            // the doctor should change it after first login.
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.Phone,
                    FullName = model.FullName,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, "Doctor@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");
                    doctor.UserId = user.Id;
                }
            }
            else
            {
                doctor.UserId = existingUser.Id;
            }

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Doctor created successfully.";
            return RedirectToAction(nameof(Doctors));
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            var vm = new DoctorFormViewModel
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Email = doctor.Email,
                Phone = doctor.Phone,
                ExperienceYears = doctor.ExperienceYears,
                SpecialtyId = doctor.SpecialtyId,
                ExistingImagePath = doctor.ImagePath,
                Specialties = await GetSpecialtySelectListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(DoctorFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Specialties = await GetSpecialtySelectListAsync();
                return View(model);
            }

            var doctor = await _context.Doctors.FindAsync(model.Id);
            if (doctor == null) return NotFound();

            doctor.FullName = model.FullName;
            doctor.Email = model.Email;
            doctor.Phone = model.Phone;
            doctor.ExperienceYears = model.ExperienceYears;
            doctor.SpecialtyId = model.SpecialtyId;

            if (model.ImageFile != null)
                doctor.ImagePath = await SaveDoctorImageAsync(model.ImageFile);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Doctor updated successfully.";
            return RedirectToAction(nameof(Doctors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            var hasAppointments = await _context.Appointments.AnyAsync(a => a.DoctorId == id);
            if (hasAppointments)
            {
                TempData["Error"] = "Cannot delete a doctor who has existing appointments.";
                return RedirectToAction(nameof(Doctors));
            }

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Doctor deleted successfully.";
            return RedirectToAction(nameof(Doctors));
        }

        private async Task<string> SaveDoctorImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "doctors");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        private async Task<IEnumerable<SelectListItem>> GetSpecialtySelectListAsync()
        {
            return await _context.Specialties
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();
        }

        // ---------- Specialties CRUD ----------

        public async Task<IActionResult> Specialties()
        {
            var specialties = await _context.Specialties
                .Select(s => new SpecialtyFormViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    DoctorCount = s.Doctors.Count
                })
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(specialties);
        }

        [HttpGet]
        public IActionResult CreateSpecialty() => View(new SpecialtyFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSpecialty(SpecialtyFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Specialties.AnyAsync(s => s.Name == model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "A specialty with this name already exists.");
                return View(model);
            }

            _context.Specialties.Add(new Specialty { Name = model.Name, Description = model.Description });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Specialty created successfully.";
            return RedirectToAction(nameof(Specialties));
        }

        [HttpGet]
        public async Task<IActionResult> EditSpecialty(int id)
        {
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null) return NotFound();

            return View(new SpecialtyFormViewModel { Id = specialty.Id, Name = specialty.Name, Description = specialty.Description });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecialty(SpecialtyFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var specialty = await _context.Specialties.FindAsync(model.Id);
            if (specialty == null) return NotFound();

            specialty.Name = model.Name;
            specialty.Description = model.Description;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Specialty updated successfully.";
            return RedirectToAction(nameof(Specialties));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpecialty(int id)
        {
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null) return NotFound();

            var hasDoctors = await _context.Doctors.AnyAsync(d => d.SpecialtyId == id);
            if (hasDoctors)
            {
                TempData["Error"] = "Cannot delete a specialty that has doctors assigned to it.";
                return RedirectToAction(nameof(Specialties));
            }

            _context.Specialties.Remove(specialty);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Specialty deleted successfully.";
            return RedirectToAction(nameof(Specialties));
        }

        // ---------- Patients ----------

        public async Task<IActionResult> Patients()
        {
            var patients = await _userManager.GetUsersInRoleAsync("Patient");
            var appointmentCounts = await _context.Appointments
                .GroupBy(a => a.PatientId)
                .Select(g => new { PatientId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PatientId, x => x.Count);

            var vm = patients
                .OrderBy(p => p.FullName)
                .Select(p => new PatientListItemViewModel
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Email = p.Email ?? string.Empty,
                    PhoneNumber = p.PhoneNumber,
                    TotalAppointments = appointmentCounts.TryGetValue(p.Id, out var count) ? count : 0
                }).ToList();

            return View(vm);
        }

        // ---------- All Appointments ----------

        public async Task<IActionResult> Appointments(string? statusFilter)
        {
            var query = _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
                .Include(a => a.Patient)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<AppointmentStatus>(statusFilter, out var status))
                query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new AppointmentListItemViewModel
                {
                    Id = a.Id,
                    DoctorName = a.Doctor!.FullName,
                    SpecialtyName = a.Doctor.Specialty!.Name,
                    PatientName = a.Patient!.FullName,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToListAsync();

            var vm = new AdminAppointmentFilterViewModel
            {
                StatusFilter = statusFilter,
                StatusOptions = Enum.GetNames(typeof(AppointmentStatus))
                    .Select(s => new SelectListItem { Value = s, Text = s }),
                Appointments = appointments
            };

            return View(vm);
        }
    }
}
