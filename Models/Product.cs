using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GunDammvc.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [StringLength(100)]
        [Display(Name = "Tên Sản Phẩm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc.")]
        [Column(TypeName = "numeric(18,2)")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Display(Name = "Mô Tả")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "URL Hình Ảnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cấp độ là bắt buộc.")]
        [Display(Name = "Cấp Độ")]
        public string Grade { get; set; } = string.Empty; // HG, RG, MG, PG

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc.")]
        [Display(Name = "Tồn Kho")]
        public int Stock { get; set; }
    }
}