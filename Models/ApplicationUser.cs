using Microsoft.AspNetCore.Identity;

namespace MediCare.Models
{
    // Extends the default Identity user with a display name shared
    // across Admin, Doctor and Patient accounts.
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
