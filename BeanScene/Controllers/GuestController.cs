using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class GuestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
