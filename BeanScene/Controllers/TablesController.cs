using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class TablesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
