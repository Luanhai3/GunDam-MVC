using Microsoft.AspNetCore.Mvc;

namespace GunDammvc.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}