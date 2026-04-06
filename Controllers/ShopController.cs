using Microsoft.AspNetCore.Mvc;
using GunDammvc.Data;
using System.Linq;

namespace GunDammvc.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }
            
            // 🎯 Lấy danh sách sản phẩm liên quan (cùng Grade)
            var relatedProducts = _context.Products
                .Where(p => p.Grade == product.Grade && p.Id != product.Id)
                .Take(4)
                .ToList();
                
            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}