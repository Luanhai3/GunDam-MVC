using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using GunDammvc.Data;
using GunDammvc.Models;
using GunDammvc.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GunDammvc.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // =============================
        // 🧠 GET OR CREATE CART
        // =============================
        private async Task<ShoppingCart> GetOrCreateCartAsync(string userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId,
                    CartItems = new List<CartItem>()
                };

                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // =============================
        // 🛒 CART PAGE
        // =============================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cart = await GetOrCreateCartAsync(userId);

            return View(cart);
        }

        // =============================
        // 🔄 UPDATE MINI CART AJAX
        // =============================
        [HttpPost]
        public async Task<IActionResult> UpdateMiniCartQuantity(int cartItemId, int quantity)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MiniCart"); // Tự động redirect về hàm GET MiniCart trả ra HTML mới
        }

        // =============================
        // 🚀 MINI CART OFFCANVAS
        // =============================
        [HttpGet]
        public async Task<IActionResult> MiniCart()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Content("<div class='p-4 text-center'>Vui lòng đăng nhập để xem giỏ hàng.</div>", "text/html");
            }
            
            var cart = await GetOrCreateCartAsync(userId);
            return PartialView("_MiniCart", cart);
        }

        // =============================
        // ➕ ADD TO CART
        // =============================
        [HttpPost, HttpGet] // Cho phép cả GET để tránh văng lỗi 405 khi gõ thẳng URL
        public async Task<IActionResult> Add(int? id, int? productId, int quantity = 1)
        {
            // Nếu gõ thẳng URL (lệnh GET), tự động đá người dùng về cửa hàng
            if (Request.Method == "GET")
            {
                return RedirectToAction("Index", "Shop");
            }

            int finalId = id ?? productId ?? 0;
            if (finalId == 0) return RedirectToAction("Index", "Home");

            var userId = _userManager.GetUserId(User);
            var cart = await GetOrCreateCartAsync(userId);

            if (quantity <= 0) quantity = 1;

            var existingItem = cart.CartItems.FirstOrDefault(c => c.ProductId == finalId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    ShoppingCartId = cart.Id,
                    ProductId = finalId,
                    Quantity = quantity
                };

                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("AddToCart", new { id = finalId }); 
        }

        // =============================
        // ⚡ ADD TO CART AJAX (Trượt Mini Cart)
        // =============================
        [HttpPost]
        public async Task<IActionResult> AddAjax(int id, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized(); // 401 nếu chưa đăng nhập

            if (quantity <= 0) quantity = 1;

            var cart = await GetOrCreateCartAsync(userId);
            var existingItem = cart.CartItems.FirstOrDefault(c => c.ProductId == id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    ShoppingCartId = cart.Id,
                    ProductId = id,
                    Quantity = quantity
                };
                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MiniCart"); // Tự động trả về View của MiniCart
        }

        // =============================
        // ✅ ADD TO CART SUCCESS PAGE
        // =============================
        [HttpGet, HttpPost] // Bổ sung HttpPost để tự động cứu lỗi Form HTML
        public async Task<IActionResult> AddToCart(int id, int quantity = 1) 
        {
            // Nếu Form HTML của bạn ghi nhầm asp-action="AddToCart" (lệnh POST)
            // Hàm này sẽ tự động chuyển dữ liệu qua hàm Add để xử lý mượt mà thay vì báo lỗi
            if (Request.Method == "POST")
            {
                return await Add(id, null, quantity);
            }

            if (id == 0) return RedirectToAction("Index"); // Nếu không có ID, tự động quay về giỏ hàng

            var userId = _userManager.GetUserId(User);
            var cart = await GetOrCreateCartAsync(userId);
            var addedProduct = await _context.Products.FindAsync(id);

            ViewBag.AddedProduct = addedProduct;
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // =============================
        // 💳 CHECKOUT PAGE
        // =============================
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var cart = await GetOrCreateCartAsync(userId);

            if (!cart.CartItems.Any())
                return RedirectToAction("Index");

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string FullName, string PhoneNumber, string Address, string PaymentMethod)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var cart = await GetOrCreateCartAsync(userId);

            if (!cart.CartItems.Any())
                return RedirectToAction("Index");

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending", // Gán trạng thái mặc định khi đặt hàng

                // Lưu các thông tin giao hàng vào model
                FirstName = FullName ?? "",
                LastName = " ", // Dùng khoảng trắng để vượt qua validation Required của LastName
                PhoneNumber = PhoneNumber ?? "",
                AddressLine1 = Address ?? "",
                City = "N/A", // Các trường bắt buộc khác của model
                ZipCode = "00000",
                Email = user?.Email ?? "no-email@domain.com",
                OrderTotal = cart.CartItems.Sum(item => item.Product.Price * item.Quantity),

                OrderItems = cart.CartItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // NẾU LÀ VNPAY THÌ CHUYỂN HƯỚNG SANG CỔNG THANH TOÁN
            if (PaymentMethod == "VNPay")
            {
                string vnp_Returnurl = _configuration["VNPay:ReturnUrl"]; // URL nhận kết quả trả về 
                string vnp_Url = _configuration["VNPay:Url"]; // URL thanh toán của VNPay 
                string vnp_TmnCode = _configuration["VNPay:TmnCode"]; // Mã website tại VNPay 
                string vnp_HashSecret = _configuration["VNPay:HashSecret"]; // Chuỗi bí mật 

                var orderTotal = cart.CartItems.Sum(item => item.Product.Price * item.Quantity);

                VNPayLibrary vnpay = new VNPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(orderTotal * 100)).ToString()); // Số tiền thanh toán (VND) nhân 100
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + order.OrderId);
                vnpay.AddRequestData("vnp_OrderType", "other"); // Loại hàng hóa
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString()); // Mã tham chiếu (mã đơn)

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                return Redirect(paymentUrl);
            }

            // Nếu không phải VNPay (COD/Bank) thì xóa giỏ và qua trang thành công luôn
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
            return RedirectToAction("OrderSuccess", new { id = order.OrderId });
        }

        // =============================
        // 🔄 VNPAY CALLBACK
        // =============================
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            var vnpayData = Request.Query;
            VNPayLibrary vnpay = new VNPayLibrary();
            foreach (var (key, value) in vnpayData)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = Request.Query["vnp_SecureHash"];

            string hashSecret = _configuration["VNPay:HashSecret"];
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);

            if (checkSignature)
            {
                if (vnp_ResponseCode == "00")
                {
                    var order = await _context.Orders.FindAsync((int)orderId);
                    if (order != null)
                    {
                        order.Status = "Paid"; // Đánh dấu đã thanh toán
                        await _context.SaveChangesAsync();
                    }
                    TempData["SuccessMessage"] = "Thanh toán đơn hàng qua VNPay thành công!";
                    return RedirectToAction("OrderSuccess", new { id = orderId });
                }
            }
            
            TempData["ErrorMessage"] = "Thanh toán thất bại hoặc có lỗi xảy ra.";
            return RedirectToAction("History");
        }

        public async Task<IActionResult> OrderSuccess(int? id)
        {
            if (id == null) return View();

            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            return View(order);
        }

        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order != null && order.Status == "Pending")
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
            }

            return RedirectToAction("History");
        }
    }
}