using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Areas.Admin.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
