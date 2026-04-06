using GunDammvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GunDammvc.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace GunDammvc.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        // ===============================
        // 📊 THỐNG KÊ (DASHBOARD)
        // ===============================
        public async Task<IActionResult> Dashboard(int? year)
        {
            var currentYear = year ?? DateTime.UtcNow.Year;
            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.OrderDate.Year == currentYear && o.Status == "Completed") // Chỉ tính đơn "Giao Thành Công"
                .ToListAsync();

            var monthlyRevenue = new decimal[12];
            var monthlyOrders = new int[12];
            var paymentMethodCounts = new int[3];

            foreach (var order in orders)
            {
                var monthIndex = order.OrderDate.Month - 1;
                monthlyRevenue[monthIndex] += order.OrderTotal;
                monthlyOrders[monthIndex]++;

                if (order.PaymentMethodSelection == PaymentMethod.COD) paymentMethodCounts[0]++;
                else if (order.PaymentMethodSelection == PaymentMethod.BankTransfer) paymentMethodCounts[1]++;
                else if (order.PaymentMethodSelection == PaymentMethod.VNPay) paymentMethodCounts[2]++;
            }

            // --- TÍNH TỔNG DOANH THU VÀ TỔNG ĐIỂM ĐÃ CẤP ---
            var totalRevenue = orders.Sum(o => o.OrderTotal);
            var averageOrderValue = orders.Any() ? totalRevenue / orders.Count : 0;
            var uniqueCustomersCount = orders.Select(o => o.UserId).Distinct().Count();

            // --- TÍNH TỶ LỆ TĂNG TRƯỞNG DOANH THU SO VỚI THÁNG TRƯỚC ---
            int targetMonth = currentYear == DateTime.UtcNow.Year ? DateTime.UtcNow.Month : 12;
            decimal currentMonthRev = monthlyRevenue[targetMonth - 1];
            decimal previousMonthRev = 0;

            if (targetMonth > 1)
            {
                previousMonthRev = monthlyRevenue[targetMonth - 2];
            }
            else
            {
                previousMonthRev = await _context.Orders
                    .Where(o => o.OrderDate.Year == currentYear - 1 && o.OrderDate.Month == 12 && o.Status == "Completed")
                    .SumAsync(o => o.OrderTotal);
            }
            double revenueGrowth = previousMonthRev > 0 ? (double)((currentMonthRev - previousMonthRev) / previousMonthRev) * 100 : (currentMonthRev > 0 ? 100 : 0);
                
            var totalPointsIssued = await _userManager.Users.SumAsync(u => u.TotalPoints);

            // --- TÍNH TỔNG ĐƠN HÀNG ĐANG CHỜ XỬ LÝ ---
            var pendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == "Pending");

            // --- TÍNH TỶ LỆ HỦY ĐƠN HÀNG ---
            int totalOrdersCount = await _context.Orders.CountAsync(o => o.OrderDate.Year == currentYear);
            int cancelledOrdersCount = await _context.Orders.CountAsync(o => o.OrderDate.Year == currentYear && o.Status == "Cancelled");
            double cancellationRate = totalOrdersCount > 0 ? (double)cancelledOrdersCount / totalOrdersCount * 100 : 0;

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.MonthlyOrders = monthlyOrders;
            ViewBag.CurrentYear = currentYear;
            ViewBag.PaymentMethodCounts = paymentMethodCounts;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AverageOrderValue = averageOrderValue;
            ViewBag.UniqueCustomersCount = uniqueCustomersCount;
            ViewBag.RevenueGrowth = revenueGrowth;
            ViewBag.CancellationRate = cancellationRate;
            ViewBag.TotalPointsIssued = totalPointsIssued;
            ViewBag.PendingOrdersCount = pendingOrdersCount;

            // --- LẤY DANH SÁCH CÁC NĂM CÓ ĐƠN HÀNG ---
            var availableYears = await _context.Orders.Select(o => o.OrderDate.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            if (!availableYears.Contains(DateTime.UtcNow.Year))
            {
                availableYears.Insert(0, DateTime.UtcNow.Year);
            }
            ViewBag.AvailableYears = availableYears;

            // --- LẤY 5 ĐƠN HÀNG MỚI NHẤT ---
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentOrders = recentOrders;

            // --- LẤY TOP 5 KHÁCH HÀNG MUA NHIỀU NHẤT TRONG NĂM ---
            var topCustomers = orders
                .GroupBy(o => new { o.UserId, o.Email, Tier = o.User?.MembershipTier ?? "Đồng" })
                .Select(g => Tuple.Create(
                    string.IsNullOrWhiteSpace(g.First().FirstName + g.First().LastName) ? "Khách Hàng" : (g.First().FirstName + " " + g.First().LastName).Trim(),
                    g.Key.Email ?? "no-email",
                    g.Sum(o => o.OrderTotal),
                    g.Count(),
                    g.Key.Tier
                ))
                .OrderByDescending(t => t.Item3)
                .Take(5)
                .ToList();
            ViewBag.TopCustomers = topCustomers;

            return View();
        }

        // ===============================
        // 📊 API LẤY THỐNG KÊ (AJAX)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats(int? year)
        {
            var currentYear = year ?? DateTime.UtcNow.Year;
            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.OrderDate.Year == currentYear && o.Status == "Completed")
                .ToListAsync();

            var pendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == "Pending");

            var monthlyRevenue = new decimal[12];
            var monthlyOrders = new int[12];
            var paymentMethodCounts = new int[3];

            foreach (var order in orders)
            {
                var monthIndex = order.OrderDate.Month - 1;
                monthlyRevenue[monthIndex] += order.OrderTotal;
                monthlyOrders[monthIndex]++;

                if (order.PaymentMethodSelection == PaymentMethod.COD) paymentMethodCounts[0]++;
                else if (order.PaymentMethodSelection == PaymentMethod.BankTransfer) paymentMethodCounts[1]++;
                else if (order.PaymentMethodSelection == PaymentMethod.VNPay) paymentMethodCounts[2]++;
            }

            var totalRevenue = orders.Sum(o => o.OrderTotal);
            var averageOrderValue = orders.Any() ? totalRevenue / orders.Count : 0;
            var uniqueCustomersCount = orders.Select(o => o.UserId).Distinct().Count();

            // --- TÍNH TỶ LỆ HỦY ĐƠN HÀNG ---
            int totalOrdersCount = await _context.Orders.CountAsync(o => o.OrderDate.Year == currentYear);
            int cancelledOrdersCount = await _context.Orders.CountAsync(o => o.OrderDate.Year == currentYear && o.Status == "Cancelled");
            double cancellationRate = totalOrdersCount > 0 ? (double)cancelledOrdersCount / totalOrdersCount * 100 : 0;

            // --- TÍNH TỶ LỆ TĂNG TRƯỞNG DOANH THU SO VỚI THÁNG TRƯỚC ---
            int targetMonth = currentYear == DateTime.UtcNow.Year ? DateTime.UtcNow.Month : 12;
            decimal currentMonthRev = monthlyRevenue[targetMonth - 1];
            decimal previousMonthRev = 0;

            if (targetMonth > 1)
            {
                previousMonthRev = monthlyRevenue[targetMonth - 2];
            }
            else
            {
                previousMonthRev = await _context.Orders
                    .Where(o => o.OrderDate.Year == currentYear - 1 && o.OrderDate.Month == 12 && o.Status == "Completed")
                    .SumAsync(o => o.OrderTotal);
            }
            double revenueGrowth = previousMonthRev > 0 ? (double)((currentMonthRev - previousMonthRev) / previousMonthRev) * 100 : (currentMonthRev > 0 ? 100 : 0);

            var topCustomers = orders
                .GroupBy(o => new { o.UserId, o.Email, Tier = o.User?.MembershipTier ?? "Đồng" })
                .Select(g => new {
                    name = string.IsNullOrWhiteSpace(g.First().FirstName + g.First().LastName) ? "Khách Hàng" : (g.First().FirstName + " " + g.First().LastName).Trim(),
                    email = g.Key.Email ?? "no-email",
                    rawTotal = g.Sum(o => o.OrderTotal),
                    totalSpent = g.Sum(o => o.OrderTotal).ToString("N0") + " đ",
                    orderCount = g.Count(),
                    tier = g.Key.Tier
                })
                .OrderByDescending(c => c.rawTotal)
                .Take(5)
                .ToList();

            return Json(new {
                totalRevenue = totalRevenue.ToString("N0") + " đ",
                totalOrders = orders.Count + " Đơn",
                averageOrderValue = averageOrderValue.ToString("N0") + " đ",
                uniqueCustomersCount = uniqueCustomersCount,
                revenueGrowth = revenueGrowth,
                cancellationRate = cancellationRate.ToString("0.1"),
                pendingOrdersCount = pendingOrdersCount,
                monthlyRevenue = monthlyRevenue,
                monthlyOrders = monthlyOrders,
                paymentMethodCounts = paymentMethodCounts,
                topCustomers = topCustomers
            });
        }

        // ===============================
        // 📥 XUẤT FILE EXCEL (CSV)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> ExportDashboardCsv(int? year)
        {
            var currentYear = year ?? DateTime.UtcNow.Year;
            var orders = await _context.Orders
                .Where(o => o.OrderDate.Year == currentYear && o.Status == "Completed")
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var csvBuilder = new System.Text.StringBuilder();
            // Thêm BOM (Byte Order Mark) để Excel mở không bị lỗi font tiếng Việt
            csvBuilder.Append('\uFEFF');
            csvBuilder.AppendLine("Mã Đơn Hàng,Ngày Đặt Hàng,Khách Hàng,Số Điện Thoại,Doanh Thu (VNĐ)");

            foreach (var order in orders)
            {
                // Xóa dấu phẩy trong tên và số điện thoại để tránh làm hỏng cấu trúc cột của file CSV
                var customerName = $"{order.FirstName} {order.LastName}".Replace(",", " ").Trim();
                var phone = order.PhoneNumber?.Replace(",", " ") ?? "";
                csvBuilder.AppendLine($"#{order.OrderId},{order.OrderDate:dd/MM/yyyy HH:mm},{customerName},{phone},{order.OrderTotal}");
            }

            csvBuilder.AppendLine($"Tổng Cộng,{orders.Count} Đơn,,,{orders.Sum(o => o.OrderTotal)}");

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(buffer, "text/csv", $"BaoCaoDoanhThu_{currentYear}.csv");
        }

        // ===============================
        // 📦 QUẢN LÝ SẢN PHẨM (INDEX)
        // ===============================
        public async Task<IActionResult> Index(string searchQuery, string grade, int page = 1)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) || p.Grade.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(grade))
            {
                query = query.Where(p => p.Grade == grade);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            ViewBag.AdminUserIds = adminUsers.Select(u => u.Id).ToList();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.Grade = grade;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(products);
        }

        // ===============================
        // 📥 XUẤT FILE EXCEL SẢN PHẨM (CSV)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> ExportProductsCsv(string searchQuery, string grade)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) || p.Grade.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(grade))
            {
                query = query.Where(p => p.Grade == grade);
            }

            var products = await query.OrderByDescending(p => p.Id).ToListAsync();

            var csvBuilder = new System.Text.StringBuilder();
            // Thêm BOM (Byte Order Mark) để Excel mở không bị lỗi font tiếng Việt
            csvBuilder.Append('\uFEFF');
            csvBuilder.AppendLine("Mã Sản Phẩm,Tên Sản Phẩm,Cấp Độ,Giá (VNĐ),Tồn Kho");

            foreach (var product in products)
            {
                var name = (product.Name ?? "").Replace(",", " ");
                var productGrade = product.Grade ?? "";
                csvBuilder.AppendLine($"{product.Id},{name},{productGrade},{product.Price},{product.Stock}");
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(buffer, "text/csv", $"DanhSachSanPham_{DateTime.UtcNow:ddMMyyyy}.csv");
        }

        // ===============================
        // ➕ THÊM SẢN PHẨM (CREATE)
        // ===============================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Description,ImageUrl,Grade,Stock")] Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    string extension = Path.GetExtension(imageFile.FileName);
                    // Thêm timestamp để tránh trùng tên ảnh
                    fileName = fileName + DateTime.UtcNow.ToString("yymmssfff") + extension; 
                    string path = Path.Combine(wwwRootPath, "images", "products");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    string fullPath = Path.Combine(path, fileName);
                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + fileName;
                }
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // ===============================
        // ✏️ SỬA SẢN PHẨM (EDIT)
        // ===============================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,ImageUrl,Grade,Stock")] Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                        string extension = Path.GetExtension(imageFile.FileName);
                        fileName = fileName + DateTime.UtcNow.ToString("yymmssfff") + extension;
                        string path = Path.Combine(wwwRootPath, "images", "products");
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        string fullPath = Path.Combine(path, fileName);
                        using (var fileStream = new FileStream(fullPath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        product.ImageUrl = "/images/products/" + fileName;
                    }
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // ===============================
        // 🔍 CHI TIẾT SẢN PHẨM (DETAILS)
        // ===============================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // ===============================
        // 🗑️ XÓA SẢN PHẨM (DELETE)
        // ===============================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 🗑️ XÓA HÀNG LOẠT SẢN PHẨM
        // ===============================
        [HttpPost]
        public async Task<IActionResult> BulkDeleteProducts(List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào được chọn.";
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var id in productIds)
            {
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    try
                    {
                        _context.Products.Remove(product);
                        await _context.SaveChangesAsync();
                        successCount++;
                    }
                    catch (DbUpdateException)
                    {
                        _context.Entry(product).State = EntityState.Unchanged;
                        failCount++;
                    }
                }
            }

            if (failCount > 0)
            {
                TempData["WarningMessage"] = $"Đã xóa {successCount} sản phẩm. Không thể xóa {failCount} sản phẩm do vướng dữ liệu liên đới (đơn hàng, đánh giá...).";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã xóa thành công {successCount} sản phẩm.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 📦 QUẢN LÝ ĐƠN HÀNG (DASHBOARD)
        // ===============================
        public async Task<IActionResult> Orders(string paymentMethod, string status, string searchQuery, int page = 1)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.Trim().ToLower();
                if (int.TryParse(search.Replace("#", ""), out int orderId))
                {
                    query = query.Where(o => o.OrderId == orderId || o.PhoneNumber.Contains(search));
                }
                else
                {
                    query = query.Where(o => (o.FirstName + " " + o.LastName).ToLower().Contains(search) || o.PhoneNumber.Contains(search));
                }
            }

            if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<GunDammvc.Models.PaymentMethod>(paymentMethod, out var pm))
            {
                query = query.Where(o => o.PaymentMethodSelection == pm);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await query.OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.Status = status;
            ViewBag.SearchQuery = searchQuery;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }

        // ===============================
        // 🔄 CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        // ===============================
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order != null)
            {
                // Nếu chuyển sang trạng thái "Đã hủy" và đơn hàng có sử dụng điểm
                if (newStatus == "Cancelled" && order.Status != "Cancelled" && order.PointsUsed > 0)
                {
                    var user = await _userManager.FindByIdAsync(order.UserId);
                    if (user != null)
                    {
                        user.Points += order.PointsUsed;
                        _context.Add(new PointHistory
                        {
                            UserId = user.Id,
                            PointsChanged = order.PointsUsed,
                            Reason = $"Hoàn điểm do Admin hủy đơn hàng #{order.OrderId}",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Nếu chuyển sang trạng thái "Đã hủy", hoàn trả số lượng sản phẩm về kho
                if (newStatus == "Cancelled" && order.Status != "Cancelled")
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Stock += item.Quantity;
                            _context.Products.Update(item.Product);
                        }
                    }
                }

                order.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật đơn hàng #{orderId} thành {newStatus}!";
            }

            return RedirectToAction(nameof(Orders));
        }

        // ===============================
        // 🔄 CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (AJAX)
        // ===============================
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatusAjax(int orderId, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
                
            if (order != null)
            {
                // Nếu chuyển sang trạng thái "Đã hủy" và đơn hàng có sử dụng điểm
                if (newStatus == "Cancelled" && order.Status != "Cancelled" && order.PointsUsed > 0)
                {
                    var user = await _userManager.FindByIdAsync(order.UserId);
                    if (user != null)
                    {
                        user.Points += order.PointsUsed;
                        _context.Add(new PointHistory
                        {
                            UserId = user.Id,
                            PointsChanged = order.PointsUsed,
                            Reason = $"Hoàn điểm do Admin hủy đơn hàng #{order.OrderId}",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Nếu chuyển sang trạng thái "Đã hủy", hoàn trả số lượng sản phẩm về kho
                if (newStatus == "Cancelled" && order.Status != "Cancelled")
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Stock += item.Quantity;
                            _context.Products.Update(item.Product);
                        }
                    }
                }

                order.Status = newStatus;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
        }

        // ===============================
        // 👥 QUẢN LÝ NGƯỜI DÙNG
        // ===============================
        public async Task<IActionResult> Users(string searchQuery, int page = 1)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.Trim().ToLower();
                query = query.Where(u => 
                    (u.FullName != null && u.FullName.ToLower().Contains(search)) || 
                    (u.Email != null && u.Email.ToLower().Contains(search)) || 
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(users);
        }

        // ===============================
        // ⭐ CẬP NHẬT ĐIỂM NGƯỜI DÙNG
        // ===============================
        [HttpPost]
        public async Task<IActionResult> UpdateUserPoints(string userId, int points)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                int oldPoints = user.Points;
                user.Points = points >= 0 ? points : 0; // Đảm bảo điểm không bị âm
                
                int diff = user.Points - oldPoints;
                if (diff != 0)
                {
                    if (diff > 0)
                    {
                        user.TotalPoints += diff; // Cập nhật cả điểm tích lũy nếu Admin cộng điểm
                    }

                    _context.Add(new PointHistory 
                    {
                        UserId = user.Id,
                        PointsChanged = diff,
                        Reason = "Quản trị viên điều chỉnh điểm",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"Đã cập nhật điểm cho {user.Email} thành {user.Points} điểm.";
            }

            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 🔒 KHÓA / MỞ KHÓA TÀI KHOẢN
        // ===============================
        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Id == _userManager.GetUserId(User))
                {
                    TempData["ErrorMessage"] = "Bạn không thể khóa tài khoản của chính mình.";
                    return RedirectToAction(nameof(Users));
                }

                await _userManager.SetLockoutEnabledAsync(user, true);
                if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Email}.";
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Email}.";
                }
            }

            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 🔐 KHÓA / MỞ KHÓA HÀNG LOẠT
        // ===============================
        [HttpPost]
        public async Task<IActionResult> BulkToggleUserLock(List<string> userIds, string actionType)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "Không có người dùng nào được chọn.";
                return RedirectToAction(nameof(Users));
            }

            int successCount = 0;
            var currentUserId = _userManager.GetUserId(User);

            foreach (var userId in userIds)
            {
                if (userId == currentUserId) continue; // Bỏ qua tự khóa chính mình

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    if (actionType == "lock")
                    {
                        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                        successCount++;
                    }
                    else if (actionType == "unlock")
                    {
                        await _userManager.SetLockoutEndDateAsync(user, null);
                        successCount++;
                    }
                }
            }

            TempData["SuccessMessage"] = $"Đã {(actionType == "lock" ? "khóa" : "mở khóa")} thành công {successCount} tài khoản.";
            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 👑 THĂNG / GIÁNG CẤP QUẢN TRỊ VIÊN
        // ===============================
        [HttpPost]
        public async Task<IActionResult> ToggleUserRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Id == _userManager.GetUserId(User))
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự thay đổi quyền của chính mình.";
                    return RedirectToAction(nameof(Users));
                }

                bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    TempData["SuccessMessage"] = $"Đã hủy quyền Quản trị viên của {user.Email}.";
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    TempData["SuccessMessage"] = $"Đã cấp quyền Quản trị viên cho {user.Email}.";
                }
            }
            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 👑 THĂNG / GIÁNG CẤP HÀNG LOẠT
        // ===============================
        [HttpPost]
        public async Task<IActionResult> BulkToggleUserRole(List<string> userIds, string roleActionType)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "Không có người dùng nào được chọn.";
                return RedirectToAction(nameof(Users));
            }

            int successCount = 0;
            var currentUserId = _userManager.GetUserId(User);

            foreach (var userId in userIds)
            {
                if (userId == currentUserId) continue; // Bỏ qua tự đổi quyền chính mình

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                    if (roleActionType == "promote" && !isAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        successCount++;
                    }
                    else if (roleActionType == "demote" && isAdmin)
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Admin");
                        successCount++;
                    }
                }
            }

            TempData["SuccessMessage"] = $"Đã {(roleActionType == "promote" ? "cấp" : "hủy")} quyền Admin thành công cho {successCount} tài khoản.";
            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 🗑️ XÓA TÀI KHOẢN NGƯỜI DÙNG
        // ===============================
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Id == _userManager.GetUserId(User))
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình.";
                    return RedirectToAction(nameof(Users));
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Đã xóa tài khoản {user.Email} thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa tài khoản. Vui lòng đảm bảo các dữ liệu liên đới (Đơn hàng, Đánh giá...) đã được xử lý trước.";
                }
            }
            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 🗑️ XÓA HÀNG LOẠT TÀI KHOẢN
        // ===============================
        [HttpPost]
        public async Task<IActionResult> BulkDeleteUsers(List<string> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "Không có người dùng nào được chọn.";
                return RedirectToAction(nameof(Users));
            }

            int successCount = 0;
            int failCount = 0;
            var currentUserId = _userManager.GetUserId(User);

            foreach (var userId in userIds)
            {
                if (userId == currentUserId) continue; // Bỏ qua tự xóa chính mình

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded) successCount++;
                    else failCount++;
                }
            }

            if (failCount > 0)
            {
                TempData["WarningMessage"] = $"Đã xóa {successCount} tài khoản. Không thể xóa {failCount} tài khoản do vướng dữ liệu liên đới.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã xóa thành công {successCount} tài khoản.";
            }
            return RedirectToAction(nameof(Users));
        }

        // ===============================
        // 📥 XUẤT FILE EXCEL NGƯỜI DÙNG (CSV)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> ExportUsersCsv(string searchQuery)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.Trim().ToLower();
                query = query.Where(u => 
                    (u.FullName != null && u.FullName.ToLower().Contains(search)) || 
                    (u.Email != null && u.Email.ToLower().Contains(search)) || 
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            var users = await query.OrderBy(u => u.Email).ToListAsync();
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserIds = adminUsers.Select(a => a.Id).ToList();

            var csvBuilder = new System.Text.StringBuilder();
            // Thêm BOM (Byte Order Mark) để Excel mở không bị lỗi font tiếng Việt
            csvBuilder.Append('\uFEFF');
            csvBuilder.AppendLine("Tên Người Dùng,Email,Số Điện Thoại,Hạng Thành Viên,Điểm Thưởng,Quyền,Trạng Thái");

            foreach (var user in users)
            {
                var fullName = (user.FullName ?? "Chưa cập nhật").Replace(",", " ");
                var email = user.Email ?? "";
                var phone = (user.PhoneNumber ?? "Chưa cập nhật").Replace(",", " ");
                var tier = user.MembershipTier ?? "Đồng";
                var role = adminUserIds.Contains(user.Id) ? "Admin" : "Khách hàng";
                var status = (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow) ? "Đã Khóa" : "Hoạt Động";

                csvBuilder.AppendLine($"{fullName},{email},{phone},{tier},{user.Points},{role},{status}");
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(buffer, "text/csv", $"DanhSachNguoiDung_{DateTime.UtcNow:ddMMyyyy}.csv");
        }

        // ===============================
        // 💬 QUẢN LÝ ĐÁNH GIÁ (REVIEWS)
        // ===============================
        public async Task<IActionResult> Reviews(string searchQuery, int page = 1)
        {
            var query = _context.Set<Review>()
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.Trim().ToLower();
                query = query.Where(r => 
                    (r.Product != null && r.Product.Name.ToLower().Contains(search)) || 
                    (r.User != null && r.User.Email != null && r.User.Email.ToLower().Contains(search)) || 
                    (r.Comment != null && r.Comment.ToLower().Contains(search)));
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reviews = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Set<Review>().FindAsync(id);
            if (review != null)
            {
                _context.Set<Review>().Remove(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa đánh giá thành công.";
            }
            return RedirectToAction(nameof(Reviews));
        }
    }
}