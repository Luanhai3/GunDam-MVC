using Microsoft.AspNetCore.Identity;

namespace GunDammvc.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public int Points { get; set; } = 0;

        public int TotalPoints { get; set; } = 0; // Tổng điểm tích lũy trọn đời

        public string MembershipTier
        {
            get
            {
                if (TotalPoints >= 5000) return "Kim Cương";
                if (TotalPoints >= 2000) return "Vàng";
                if (TotalPoints >= 500) return "Bạc";
                return "Đồng";
            }
        }
    }
}