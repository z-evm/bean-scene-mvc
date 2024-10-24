using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class MenuController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
