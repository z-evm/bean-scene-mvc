using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Formats.Asn1.AsnWriter;

namespace BeanScene.Areas.Staff.Controllers
{
    [Authorize(Roles = "Staff, Manager, Admin")] 
    [Area("Staff")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}