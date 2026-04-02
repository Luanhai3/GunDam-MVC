using System.Security.Claims;
using System.Threading.Tasks;
using GunDammvc.Data;
using GunDammvc.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GunDammvc.Models
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartService(ApplicationDbContext context,
                                   IHttpContextAccessor httpContextAccessor,
                                   UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<ShoppingCart> GetOrCreateCartAsync(ClaimsPrincipal user)
        {
            var userId = _userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("User is not authenticated");
            }

            var cart = await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task AddItemToCartAsync(ClaimsPrincipal user, int productId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(user);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    ShoppingCartId = cart.Id
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveItemFromCartAsync(ClaimsPrincipal user, int cartItemId)
        {
            var cart = await GetOrCreateCartAsync(user);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateItemQuantityAsync(ClaimsPrincipal user, int cartItemId, int newQuantity)
        {
            var cart = await GetOrCreateCartAsync(user);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem != null)
            {
                cartItem.Quantity = newQuantity;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCartItemCountAsync(ClaimsPrincipal user)
        {
            var cart = await GetOrCreateCartAsync(user);
            return cart.CartItems.Sum(ci => ci.Quantity);
        }

        public async Task ClearCartAsync(ClaimsPrincipal user)
        {
            var cart = await GetOrCreateCartAsync(user);
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
        }
    }
}
