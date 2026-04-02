using Microsoft.AspNetCore.Identity;

namespace GunDammvc.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}