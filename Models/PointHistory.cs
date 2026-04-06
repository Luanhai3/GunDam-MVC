using System;

namespace GunDammvc.Models
{
    public class PointHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PointsChanged { get; set; } // Sẽ là số dương (+) khi nhận, số âm (-) khi tiêu
        public string Reason { get; set; } = string.Empty; // Lý do thay đổi điểm
       	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ApplicationUser? User { get; set; }
    }
}