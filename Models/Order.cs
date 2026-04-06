using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GunDammvc.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int Id { get; set; } 

        [Required]
        public string UserId { get; set; } = string.Empty;
        public decimal TierDiscountAmount { get; set; } = 0;
        public int PointsUsed { get; set; } = 0;            
        public string? CouponCode { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(100)]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "Zip code is required")]
        [Display(Name = "Zip Code")]
        [StringLength(10, MinimumLength = 4)]
        public string ZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(10)]
        public string? State { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(25)]
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.EmailAddress)]
        [RegularExpression(@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])",
            ErrorMessage = "The email address is not entered in a correct format")]
        public string Email { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderTotal { get; set; }

        public DateTime OrderPlaced { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending";

        public List<OrderItem> OrderItems { get; set; } = new();

        [Required(ErrorMessage = "Please select a payment method")]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethodSelection { get; set; }
    }

    public enum PaymentMethod
    {
        [Display(Name = "Thanh toán khi nhận hàng (COD)")]
        COD = 0,
        [Display(Name = "Chuyển khoản ngân hàng")]
        BankTransfer = 1,
        [Display(Name = "Thanh toán VNPay")]
        VNPay = 2
    }
}
