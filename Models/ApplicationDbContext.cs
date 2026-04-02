using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GunDammvc.Models;

namespace GunDammvc.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            // Thiết lập mối quan hệ
            modelBuilder.Entity<ShoppingCart>()
                .HasMany(s => s.CartItems)
                .WithOne()
                .HasForeignKey(c => c.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Order>()
    .HasMany(o => o.OrderItems)
    .WithOne(oi => oi.Order)
    .HasForeignKey(oi => oi.OrderId)
    .OnDelete(DeleteBehavior.Cascade);


            // Bắt đầu thêm dữ liệu mẫu cho bảng Product
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "HG 1/144 RX-78-2 Gundam [Beyond Global]",
                    Grade = "HG",
                    Price = 250000,
                    Stock = 20,
                    Description = "A modern High Grade interpretation of the original Gundam, celebrating the 40th anniversary of Gunpla.",
                    ImageUrl = "" // Để trống để dùng ảnh placeholder
                },
                new Product
                {
                    Id = 2,
                    Name = "RG 1/144 Wing Gundam Zero EW",
                    Grade = "RG",
                    Price = 350000,
                    Stock = 15,
                    Description = "The iconic angel-winged Gundam from 'Gundam Wing: Endless Waltz', recreated in stunning Real Grade detail.",
                    ImageUrl = ""
                },
                new Product
                {
                    Id = 3,
                    Name = "MG 1/100 Sazabi Ver.Ka",
                    Grade = "MG",
                    Price = 1200000,
                    Stock = 5,
                    Description = "Char Aznable's final mobile suit, designed by Hajime Katoki. A masterpiece of engineering and design.",
                    ImageUrl = ""
                }
            );
        }
    }
}