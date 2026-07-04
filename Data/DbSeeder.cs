using MediCare.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace MediCare.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // 1. Roles
            string[] roles = { "Admin", "Doctor", "Patient" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Admin account
            const string adminEmail = "admin@medicare.com";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // 3. Specialties
            if (!await context.Specialties.AnyAsync())
            {
                context.Specialties.AddRange(
                    new Specialty { Name = "Cardiology", Description = "Diagnosis and treatment of heart conditions." },
                    new Specialty { Name = "Dermatology", Description = "Skin, hair and nail care and treatment." },
                    new Specialty { Name = "Pediatrics", Description = "Medical care for infants, children and adolescents." },
                    new Specialty { Name = "Orthopedics", Description = "Treatment of bones, joints and muscles." }
                );
                await context.SaveChangesAsync();
            }

            // 4. Doctors (each with a login account in the Doctor role)
            if (!await context.Doctors.AnyAsync())
            {
                var cardiology = await context.Specialties.FirstAsync(s => s.Name == "Cardiology");
                var dermatology = await context.Specialties.FirstAsync(s => s.Name == "Dermatology");
                var pediatrics = await context.Specialties.FirstAsync(s => s.Name == "Pediatrics");
                var orthopedics = await context.Specialties.FirstAsync(s => s.Name == "Orthopedics");

                var seedDoctors = new List<(string Name, string Email, string Phone, int Years, Specialty Specialty)>
                {
                    ("Dr. Sarah Johnson", "sarah.johnson@medicare.com", "555-0101", 12, cardiology),
                    ("Dr. Michael Chen", "michael.chen@medicare.com", "555-0102", 8, dermatology),
                    ("Dr. Emily Davis", "emily.davis@medicare.com", "555-0103", 15, pediatrics),
                    ("Dr. James Wilson", "james.wilson@medicare.com", "555-0104", 10, orthopedics),
                    ("Dr. Olivia Martinez", "olivia.martinez@medicare.com", "555-0105", 6, cardiology),
                };

                foreach (var d in seedDoctors)
                {
                    var user = new ApplicationUser
                    {
                        UserName = d.Email,
                        Email = d.Email,
                        FullName = d.Name,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, "Doctor@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Doctor");

                        context.Doctors.Add(new Doctor
                        {
                            FullName = d.Name,
                            Email = d.Email,
                            Phone = d.Phone,
                            ExperienceYears = d.Years,
                            SpecialtyId = d.Specialty.Id,
                            UserId = user.Id,
                            ImagePath = "wwwroot/images/doctors/1.png"
                        });
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
