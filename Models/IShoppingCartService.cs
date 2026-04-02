using GunDammvc.Models;
using System.Security.Claims;

namespace GunDammvc.Models
{
    public interface IShoppingCartService
    {
        Task<ShoppingCart> GetOrCreateCartAsync(ClaimsPrincipal user);
        Task AddItemToCartAsync(ClaimsPrincipal user, int productId, int quantity);
        Task RemoveItemFromCartAsync(ClaimsPrincipal user, int cartItemId);
        Task UpdateItemQuantityAsync(ClaimsPrincipal user, int cartItemId, int newQuantity);
        Task<int> GetCartItemCountAsync(ClaimsPrincipal user);
        Task ClearCartAsync(ClaimsPrincipal user);
    }
}
