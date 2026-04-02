using GunDammvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GunDammvc.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace GunDammvc.Controllers
{
    // Bạn có thể mở comment dòng dưới nếu đã thiết lập Role "Admin" trong Identity
    // [Authorize(Roles = "Admin")] 
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // ===============================
        // 📊 THỐNG KÊ (DASHBOARD)
        // ===============================
        public async Task<IActionResult> Dashboard()
        {
            var currentYear = DateTime.Now.Year;
            var orders = await _context.Orders
                .Where(o => o.OrderDate.Year == currentYear && o.Status == "Paid") // Chỉ tính đơn đã thanh toán
                .ToListAsync();

            var monthlyRevenue = new decimal[12];
            var monthlyOrders = new int[12];

            foreach (var order in orders)
            {
                var monthIndex = order.OrderDate.Month - 1;
                monthlyRevenue[monthIndex] += order.OrderTotal;
                monthlyOrders[monthIndex]++;
            }

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.MonthlyOrders = monthlyOrders;
            ViewBag.CurrentYear = currentYear;

            return View();
        }

        // ===============================
        // 📦 QUẢN LÝ SẢN PHẨM (INDEX)
        // ===============================
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
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
                    fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension; 
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
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
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
        // 📦 QUẢN LÝ ĐƠN HÀNG (DASHBOARD)
        // ===============================
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ===============================
        // 🔄 CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        // ===============================
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật đơn hàng #{orderId} thành {newStatus}!";
            }

            return RedirectToAction(nameof(Orders));
        }
    }
}