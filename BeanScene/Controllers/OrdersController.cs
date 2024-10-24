using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
