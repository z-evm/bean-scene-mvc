using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class ReservationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
