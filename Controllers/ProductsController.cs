using GunDammvc.Data;
using GunDammvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GunDammvc.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // 📌 DANH SÁCH SẢN PHẨM
        // ===============================
        public async Task<IActionResult> Index(decimal? minPrice, decimal? maxPrice, string grade, string sortOrder, int page = 1)
        {
            var query = _context.Products.AsQueryable();

            // Lọc theo Grade
            if (!string.IsNullOrEmpty(grade))
            {
                query = query.Where(p => p.Grade == grade);
            }

            // Lọc theo giá tối thiểu
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            // Lọc theo giá tối đa
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    break;
            }

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Grade = grade;
            ViewBag.SortOrder = sortOrder;

            // --- LOGIC PHÂN TRANG (PAGINATION) ---
            int pageSize = 6; // Số lượng sản phẩm muốn hiển thị trên 1 trang (bạn có thể tuỳ chỉnh)
            int totalItems = await query.CountAsync(); // Tổng số sản phẩm thỏa mãn điều kiện lọc
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize); // Tính tổng số trang

            // Truy vấn lấy dữ liệu theo trang
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(products);
        }

        // ===============================
        // 📌 CHI TIẾT SẢN PHẨM
        // ===============================
        public async Task<IActionResult> Details(int id, int? ratingFilter)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // 🎯 Lấy danh sách đánh giá và tính sao trung bình
            var reviewsQuery = _context.Set<Review>()
                .Include(r => r.User)
                .Where(r => r.ProductId == id);

            // Tính tổng quát trước khi lọc
            ViewBag.AverageRating = await reviewsQuery.AnyAsync() ? await reviewsQuery.AverageAsync(r => r.Rating) : 0.0;
            ViewBag.ReviewCount = await reviewsQuery.CountAsync();

            // Áp dụng bộ lọc nếu có
            if (ratingFilter.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating == ratingFilter.Value);
            }

            ViewBag.Reviews = await reviewsQuery.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.CurrentRatingFilter = ratingFilter;

            // 🎯 Sản phẩm liên quan
            var relatedProducts = await _context.Products
                .Where(p => p.Grade == product.Grade && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // ===============================
        // 📌 TÌM KIẾM SẢN PHẨM
        // ===============================
        public async Task<IActionResult> Search(string keyword)
        {
            var products = await _context.Products
                .Where(p => p.Name.Contains(keyword))
                .ToListAsync();

            ViewBag.Keyword = keyword;

            return View("Index", products);
        }

        // ===============================
        // 📌 LỌC THEO GRADE (HG, RG, MG...)
        // ===============================
        public async Task<IActionResult> Filter(string grade)
        {
            var products = await _context.Products
                .Where(p => p.Grade == grade)
                .ToListAsync();

            ViewBag.Grade = grade;

            return View("Index", products);
        }

        // ===============================
        // 👍 ĐÁNH GIÁ HỮU ÍCH
        // ===============================
        [HttpPost]
        public async Task<IActionResult> MarkReviewHelpful(int reviewId)
        {
            var review = await _context.Set<Review>().FindAsync(reviewId);
            if (review != null)
            {
                review.HelpfulCount++;
                await _context.SaveChangesAsync();
                return Json(new { success = true, newCount = review.HelpfulCount });
            }
            return Json(new { success = false, message = "Không tìm thấy đánh giá" });
        }
    }
}