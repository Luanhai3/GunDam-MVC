using System;
using System.ComponentModel.DataAnnotations;

namespace GunDammvc.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        public decimal DiscountAmount { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}