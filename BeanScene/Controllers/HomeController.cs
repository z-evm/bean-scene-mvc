using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
