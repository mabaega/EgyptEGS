using Microsoft.AspNetCore.Mvc;

namespace TokenServices.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
