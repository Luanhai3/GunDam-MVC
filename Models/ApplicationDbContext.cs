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
        public DbSet<Coupon> Coupons { get; set; }

        // DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<PointHistory> PointHistories { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            // ShoppingCart → CartItems
            modelBuilder.Entity<ShoppingCart>()
                .HasMany(s => s.CartItems)
                .WithOne()
                .HasForeignKey(c => c.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Order → OrderItems
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // (Optional nhưng nên có) PointHistory → User
            modelBuilder.Entity<PointHistory>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Product
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "HG 1/144 RX-78-2 Gundam [Beyond Global]",
                    Grade = "HG",
                    Price = 250000,
                    Stock = 20,
                    Description = "A modern High Grade interpretation of the original Gundam.",
                    ImageUrl = ""
                },
                new Product
                {
                    Id = 2,
                    Name = "RG 1/144 Wing Gundam Zero EW",
                    Grade = "RG",
                    Price = 350000,
                    Stock = 15,
                    Description = "Iconic angel-winged Gundam.",
                    ImageUrl = ""
                },
                new Product
                {
                    Id = 3,
                    Name = "MG 1/100 Sazabi Ver.Ka",
                    Grade = "MG",
                    Price = 1200000,
                    Stock = 5,
                    Description = "Char Aznable's final mobile suit.",
                    ImageUrl = ""
                }
            );
        }
    }
}