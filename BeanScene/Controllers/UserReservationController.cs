using BeanScene.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BeanScene.Controllers
{
    public class UserReservationController : Controller
    {
        private readonly ILogger<UserReservationController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public UserReservationController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<UserReservationController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userEmail = user.Email;

            var reservations = await _context.Reservations
                .Include(r => r.Sitting)
                .Include(r => r.Person)
                .Where(r => r.Person.Email == userEmail)
                .OrderByDescending(r => r.Start)
                .ToListAsync();

            return View(reservations);
        }
    }
}
