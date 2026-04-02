using Microsoft.AspNetCore.Mvc;
using GunDammvc.Models;

namespace GunDammvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;

        public HomeController(IProductService productService)
        {
            _productService = productService;
        }

        // Hiển thị danh sách Gundam và Lọc theo Grade
        public async Task<IActionResult> Index(string? grade)
        {
            ViewData["CurrentGrade"] = grade;
            var products = await _productService.GetProductsAsync(grade);
            return View(products);
        }
    }
}