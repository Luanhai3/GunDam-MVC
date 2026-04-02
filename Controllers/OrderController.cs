using GunDammvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GunDammvc.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderRepository _orderRepository;

        public OrderController(IShoppingCartService shoppingCartService, IOrderRepository orderRepository)
        {
            _shoppingCartService = shoppingCartService;
            _orderRepository = orderRepository;
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = await _shoppingCartService.GetOrCreateCartAsync(User);
            if (cart.CartItems.Count == 0)
            {
                ModelState.AddModelError("", "Your cart is empty, add some products first");
            }

            if (ModelState.IsValid)
            {
                await _orderRepository.CreateOrderAsync(User, order);
                TempData["SuccessMessage"] = "Your order has been placed successfully!";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}
