using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Areas.Staff.Controllers
{
    [Authorize(Roles ="Staff,Admin"),Area("Staff")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
