using Microsoft.AspNetCore.Mvc;

namespace GunDammvc.Controllers
{
    public class CollectionsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}