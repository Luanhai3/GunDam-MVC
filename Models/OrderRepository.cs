using GunDammvc.Data;
using GunDammvc.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GunDammvc.Models
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IShoppingCartService _shoppingCartService;

        public OrderRepository(ApplicationDbContext context, IShoppingCartService shoppingCartService)
        {
            _context = context;
            _shoppingCartService = shoppingCartService;
        }

        public async Task CreateOrderAsync(ClaimsPrincipal user, Order order)
        {
            order.OrderPlaced = DateTime.UtcNow;
            var shoppingCart = await _shoppingCartService.GetOrCreateCartAsync(user);
            order.OrderTotal = shoppingCart.TotalPrice;
            _context.Orders.Add(order);

            var cartItems = shoppingCart.CartItems;

            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product != null)
                {
                    var orderItem = new OrderItem()
                    {
                        Quantity = cartItem.Quantity,
                        ProductId = cartItem.ProductId,
                        OrderId = order.OrderId,
                        Price = cartItem.Product.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }
            }

            await _context.SaveChangesAsync();
            await _shoppingCartService.ClearCartAsync(user);
        }
    }
}
