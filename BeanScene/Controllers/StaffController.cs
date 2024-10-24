using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
