using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Areas.Admin.Controllers
{
    public class ManagereservationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
