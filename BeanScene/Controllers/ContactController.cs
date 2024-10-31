using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
