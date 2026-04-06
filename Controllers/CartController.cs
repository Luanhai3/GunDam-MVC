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

            // --- TỰ ĐỘNG LÀM SẠCH GIỎ HÀNG NẾU TỒN KHO THAY ĐỔI ---
            bool outOfStockRemoved = false;
            bool quantityReduced = false;

            foreach (var item in cart.CartItems.ToList())
            {
                if (item.Product == null || item.Product.Stock <= 0)
                {
                    cart.CartItems.Remove(item);
                    _context.CartItems.Remove(item);
                    outOfStockRemoved = true;
                }
                else if (item.Quantity > item.Product.Stock)
                {
                    item.Quantity = item.Product.Stock;
                    quantityReduced = true;
                }
            }

            if (outOfStockRemoved || quantityReduced)
            {
                await _context.SaveChangesAsync();
                if (outOfStockRemoved) TempData["ErrorMessage"] = "Một số sản phẩm đã hết hàng và tự động bị xóa khỏi giỏ.";
                else if (quantityReduced) TempData["WarningMessage"] = "Số lượng một số sản phẩm đã được tự động điều chỉnh do giới hạn kho.";
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
            var item = await _context.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartItemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    if (item.Product.Stock <= 0)
                    {
                        _context.CartItems.Remove(item);
                        TempData["ErrorMessage"] = $"Sản phẩm {item.Product.Name} đã hết hàng.";
                    }
                    else if (quantity > item.Product.Stock)
                    {
                        item.Quantity = item.Product.Stock;
                        TempData["WarningMessage"] = $"Sản phẩm {item.Product.Name} chỉ còn {item.Product.Stock} trong kho. Số lượng đã được tự động điều chỉnh.";
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
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

            var product = await _context.Products.FindAsync(finalId);
            if (product == null) return NotFound();

            var existingItem = cart.CartItems.FirstOrDefault(c => c.ProductId == finalId);

            int currentQty = existingItem?.Quantity ?? 0;
            if (currentQty + quantity > product.Stock)
            {
                quantity = product.Stock - currentQty;
                if (quantity <= 0)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {product.Name} hiện đã đạt giới hạn tồn kho ({product.Stock}) trong giỏ hàng của bạn.";
                    return RedirectToAction("Details", "Products", new { id = finalId });
                }
                TempData["WarningMessage"] = $"Sản phẩm {product.Name} chỉ còn {product.Stock} trong kho. Số lượng đã được tự động điều chỉnh.";
            }

            if (quantity > 0)
            {
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
            }

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

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var cart = await GetOrCreateCartAsync(userId);
            var existingItem = cart.CartItems.FirstOrDefault(c => c.ProductId == id);

            int currentQty = existingItem?.Quantity ?? 0;
            if (currentQty + quantity > product.Stock)
            {
                // Tự động gán bằng số lượng tối đa còn lại
                quantity = product.Stock - currentQty;
                if (quantity <= 0)
                {
                    return BadRequest($"Sản phẩm {product.Name} hiện đã đạt giới hạn tồn kho trong giỏ hàng của bạn.");
                }
                Response.Headers.Append("X-Warning-Message", Uri.EscapeDataString($"Đã điều chỉnh: Chỉ còn tối đa {product.Stock} sản phẩm."));
            }

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
            var item = await _context.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartItemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    if (item.Product.Stock <= 0)
                    {
                        _context.CartItems.Remove(item);
                        TempData["ErrorMessage"] = $"Sản phẩm {item.Product.Name} đã hết hàng.";
                    }
                    else if (quantity > item.Product.Stock)
                    {
                        item.Quantity = item.Product.Stock;
                        TempData["WarningMessage"] = $"Sản phẩm {item.Product.Name} chỉ còn {item.Product.Stock} trong kho. Số lượng đã được tự động điều chỉnh.";
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
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
            var user = await _userManager.FindByIdAsync(userId);
            var cart = await GetOrCreateCartAsync(userId);

            if (!cart.CartItems.Any())
                return RedirectToAction("Index");

            ViewBag.AvailablePoints = user?.Points ?? 0;
            ViewBag.MembershipTier = user?.MembershipTier ?? "Đồng";
            ViewBag.TierDiscountRate = user?.MembershipTier == "Kim Cương" ? 0.10m : (user?.MembershipTier == "Vàng" ? 0.05m : (user?.MembershipTier == "Bạc" ? 0.02m : 0m));
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string FullName, string PhoneNumber, string Address, string PaymentMethod, int UsePoints = 0, string CouponCode = null)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var cart = await GetOrCreateCartAsync(userId);

            if (!cart.CartItems.Any())
                return RedirectToAction("Index");

            // --- KIỂM TRA TỒN KHO TRƯỚC KHI ĐẶT ---
            foreach (var item in cart.CartItems)
            {
                if (item.Quantity > item.Product.Stock)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {item.Product.Name} hiện chỉ còn {item.Product.Stock} trong kho. Vui lòng điều chỉnh lại số lượng.";
                    return RedirectToAction("Index");
                }
            }

            // --- TÍNH GIẢM GIÁ THEO HẠNG THÀNH VIÊN ---
            decimal tierDiscountRate = user?.MembershipTier == "Kim Cương" ? 0.10m : (user?.MembershipTier == "Vàng" ? 0.05m : (user?.MembershipTier == "Bạc" ? 0.02m : 0m));
            var subTotal = cart.CartItems.Sum(item => item.Product.Price * item.Quantity);
            decimal tierDiscountAmount = subTotal * tierDiscountRate;

            // --- XỬ LÝ TRỪ ĐIỂM THƯỞNG ---
            if (UsePoints > 0 && user != null && user.Points < UsePoints) UsePoints = user.Points; // Đảm bảo không dùng quá điểm đang có
            int discountAmount = UsePoints * 1000; // Quy đổi: 1 Điểm = 1.000 VNĐ
            
            // --- XỬ LÝ MÃ GIẢM GIÁ (COUPON) ---
            decimal couponDiscountAmount = 0;
            string appliedCouponCode = null;
            if (!string.IsNullOrWhiteSpace(CouponCode))
            {
                var codeUpper = CouponCode.Trim().ToUpper();
                
                // Kiểm tra xem user đã dùng mã này cho đơn hàng nào chưa (bỏ qua các đơn đã hủy)
                bool alreadyUsed = await _context.Orders.AnyAsync(o => o.UserId == userId && o.CouponCode == codeUpper && o.Status != "Cancelled");
                if (alreadyUsed)
                {
                    TempData["ErrorMessage"] = "Bạn đã sử dụng mã giảm giá này cho một đơn hàng trước đó.";
                    return RedirectToAction("Checkout");
                }

                var activeCoupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == codeUpper && c.IsActive);
                
                if (activeCoupon != null && (!activeCoupon.ExpiryDate.HasValue || activeCoupon.ExpiryDate.Value >= DateTime.UtcNow))
                {
                    couponDiscountAmount = activeCoupon.DiscountAmount;
                    appliedCouponCode = codeUpper;
                }
            }

            // --- TÍNH PHÍ VẬN CHUYỂN (TRUNG TÂM VŨNG TÀU) ---
            decimal shippingFee = 0;
            if (!string.IsNullOrWhiteSpace(Address))
            {
                if (Address.Contains("Vũng Tàu", StringComparison.OrdinalIgnoreCase) || Address.Contains("vung tau", StringComparison.OrdinalIgnoreCase))
                {
                    shippingFee = 20000; // Nội thành Vũng Tàu
                }
                else
                {
                    shippingFee = 40000; // Ngoại thành
                }
            }

            var finalTotal = subTotal - tierDiscountAmount - discountAmount - couponDiscountAmount + shippingFee;
            if (finalTotal < 0) 
            {
                finalTotal = 0;
                UsePoints = (int)((subTotal - tierDiscountAmount) / 1000); // Điều chỉnh lại nếu tiền hàng nhỏ hơn số điểm quy đổi
            }

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
                OrderTotal = finalTotal,
                TierDiscountAmount = tierDiscountAmount + couponDiscountAmount, // Gom chung ưu đãi hạng và mã giảm giá
                PointsUsed = UsePoints,
                CouponCode = appliedCouponCode,
                PaymentMethodSelection = PaymentMethod == "BankTransfer" ? Models.PaymentMethod.BankTransfer : (PaymentMethod == "VNPay" ? Models.PaymentMethod.VNPay : Models.PaymentMethod.COD),

                OrderItems = cart.CartItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);

            // --- TRỪ TỒN KHO SẢN PHẨM ---
            foreach (var item in cart.CartItems)
            {
                item.Product.Stock -= item.Quantity;
                _context.Products.Update(item.Product);
            }

            // --- TRỪ ĐIỂM CỦA USER NẾU CÓ SỬ DỤNG ---
            if (UsePoints > 0 && user != null)
            {
                user.Points -= UsePoints;
                _context.Add(new PointHistory
                {
                    UserId = user.Id,
                    PointsChanged = -UsePoints,
                    Reason = "Sử dụng điểm cho đơn hàng",
                    CreatedAt = DateTime.UtcNow
                });
                _context.Update(user);
            }

            await _context.SaveChangesAsync();

            // NẾU LÀ VNPAY THÌ CHUYỂN HƯỚNG SANG CỔNG THANH TOÁN
            if (PaymentMethod == "VNPay")
            {
                string vnp_Returnurl = _configuration["VNPay:ReturnUrl"]; // URL nhận kết quả trả về 
                string vnp_Url = _configuration["VNPay:Url"]; // URL thanh toán của VNPay 
                string vnp_TmnCode = _configuration["VNPay:TmnCode"]; // Mã website tại VNPay 
                string vnp_HashSecret = _configuration["VNPay:HashSecret"]; // Chuỗi bí mật 

                VNPayLibrary vnpay = new VNPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(finalTotal * 100)).ToString()); // Số tiền thanh toán (VND) nhân 100
                vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
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

            // --- TÍNH ĐIỂM THƯỞNG ---
            // Tỷ lệ quy đổi 20.000 VNĐ = 1 Điểm
            int earnedPoints = (int)(order.OrderTotal / 20000);
            if (user != null)
            {
                user.Points += earnedPoints; 
                user.TotalPoints += earnedPoints;
                _context.Add(new PointHistory
                {
                    UserId = user.Id,
                    PointsChanged = earnedPoints,
                    Reason = "Tích lũy từ đơn hàng",
                    CreatedAt = DateTime.UtcNow
                });
                _context.Update(user);
            }
            TempData["EarnedPoints"] = earnedPoints;

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

                        // --- TÍNH ĐIỂM THƯỞNG CHO VNPAY ---
                        var user = await _userManager.FindByIdAsync(order.UserId);
                        if (user != null)
                        {
                            int earnedPoints = (int)(order.OrderTotal / 20000);
                            user.Points += earnedPoints;
                            user.TotalPoints += earnedPoints;
                            _context.Add(new PointHistory
                            {
                                UserId = user.Id,
                                PointsChanged = earnedPoints,
                                Reason = "Tích lũy từ đơn hàng VNPay",
                                CreatedAt = DateTime.UtcNow
                            });
                            _context.Update(user);
                            TempData["EarnedPoints"] = earnedPoints;
                        }

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

            // Lấy danh sách ID các sản phẩm mà người dùng đã đánh giá
            var reviewedProductIds = await _context.Set<Review>()
                .Where(r => r.UserId == userId)
                .Select(r => r.ProductId)
                .Distinct()
                .ToListAsync();
            ViewBag.ReviewedProductIds = reviewedProductIds;
            
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
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order != null && order.Status == "Pending")
            {
                order.Status = "Cancelled";
                
                // Hoàn lại số điểm nếu khách có sử dụng
                if (order.PointsUsed > 0)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        user.Points += order.PointsUsed;
                        _context.Add(new PointHistory
                        {
                            UserId = user.Id,
                            PointsChanged = order.PointsUsed,
                            Reason = $"Hoàn điểm do hủy đơn hàng #{order.OrderId}",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Hoàn trả số lượng sản phẩm về kho
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.Stock += item.Quantity;
                        _context.Products.Update(item.Product);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
            }

            return RedirectToAction("History");
        }

        // =============================
        // ♻️ MUA LẠI ĐƠN HÀNG (REORDER)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Reorder(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            var cart = await GetOrCreateCartAsync(userId);
            bool hasStockIssue = false;

            foreach (var item in order.OrderItems)
            {
                if (item.Product == null || item.Product.Stock <= 0)
                {
                    hasStockIssue = true;
                    continue; // Bỏ qua sản phẩm đã bị xóa hoặc hết hàng
                }

                var existingItem = cart.CartItems.FirstOrDefault(c => c.ProductId == item.ProductId);
                int quantityToAdd = item.Quantity;

                if (existingItem != null)
                {
                    existingItem.Quantity += quantityToAdd;
                    if (existingItem.Quantity > item.Product.Stock)
                    {
                        existingItem.Quantity = item.Product.Stock;
                        hasStockIssue = true;
                    }
                }
                else
                {
                    if (quantityToAdd > item.Product.Stock)
                    {
                        quantityToAdd = item.Product.Stock;
                        hasStockIssue = true;
                    }
                    
                    var newItem = new CartItem
                    {
                        ShoppingCartId = cart.Id,
                        ProductId = item.ProductId,
                        Quantity = quantityToAdd
                    };
                    _context.CartItems.Add(newItem);
                }
            }

            await _context.SaveChangesAsync();

            if (hasStockIssue)
            {
                TempData["WarningMessage"] = "Một số sản phẩm đã hết hàng hoặc không đủ số lượng và đã được tự động điều chỉnh.";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã thêm các sản phẩm vào giỏ hàng thành công!";
            }

            return RedirectToAction("Index"); // Chuyển người dùng tới trang giỏ hàng để họ kiểm tra lại
        }

        // =============================
        // ⭐ LỊCH SỬ ĐIỂM THƯỞNG
        // =============================
        public async Task<IActionResult> PointHistory()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.CurrentPoints = user?.Points ?? 0;
            ViewBag.MembershipTier = user?.MembershipTier ?? "Đồng";

            var history = await _context.Set<PointHistory>()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(history);
        }

        // =============================
        // ⭐ ĐÁNH GIÁ SẢN PHẨM
        // =============================
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Kiểm tra xem người dùng đã mua sản phẩm này và đơn hàng đã giao thành công chưa?
            var hasPurchased = await _context.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.UserId == userId && o.Status == "Completed" && o.OrderItems.Any(i => i.ProductId == productId));

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sản phẩm sau khi đã mua và nhận hàng thành công.";
                return RedirectToAction("History");
            }

            // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa để tránh lạm dụng
            var alreadyReviewed = await _context.Set<Review>()
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);

            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi. Mỗi sản phẩm chỉ được đánh giá và nhận thưởng 1 lần duy nhất.";
                return RedirectToAction("History");
            }

            // Lưu đánh giá xuống Database
            var review = new Review { ProductId = productId, UserId = userId, Rating = rating, Comment = comment, CreatedAt = DateTime.UtcNow };
            _context.Add(review);

            // --- TẶNG ĐIỂM THƯỞNG CHO KHÁCH HÀNG ---
            int rewardPoints = 10; // Cài đặt số điểm tặng (Ví dụ 10 điểm = 10.000 VNĐ)
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.Points += rewardPoints;
                user.TotalPoints += rewardPoints;
                _context.Add(new PointHistory
                {
                    UserId = user.Id,
                    PointsChanged = rewardPoints,
                    Reason = "Tặng điểm đánh giá sản phẩm",
                    CreatedAt = DateTime.UtcNow
                });
                _context.Update(user);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cảm ơn bạn đã đánh giá! Bạn vừa được tặng {rewardPoints} điểm thưởng.";
            return RedirectToAction("History");
        }

        // =============================
        // 🎟️ API KIỂM TRA MÃ GIẢM GIÁ
        // =============================
        [HttpGet]
        public async Task<IActionResult> CheckCoupon(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Json(new { success = false, message = "Mã không hợp lệ." });
            
            var userId = _userManager.GetUserId(User);
            var codeUpper = code.Trim().ToUpper();

            var coupon = await _context.Set<Coupon>()
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == codeUpper && c.IsActive);
                
            if (coupon == null) return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa." });
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow) return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
            
            bool alreadyUsed = await _context.Orders.AnyAsync(o => o.UserId == userId && o.CouponCode == codeUpper && o.Status != "Cancelled");
            if (alreadyUsed) return Json(new { success = false, message = "Bạn đã sử dụng mã giảm giá này rồi." });

            return Json(new { success = true, discountAmount = coupon.DiscountAmount, message = $"Đã áp dụng mã giảm giá {coupon.DiscountAmount:N0}đ!" });
        }
    }
}