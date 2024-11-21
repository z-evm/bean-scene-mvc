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
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if not authenticated
            }

            // Get the user's email
            var userEmail = user.Email;

            // Fetch reservations for the user
            var reservations = await _context.Reservations
                .Include(r => r.Sitting) // Include Sitting details (e.g., date, time)
                .Include(r => r.Person) // Include Person details (e.g., name, email)
                .Where(r => r.Person.Email == userEmail) // Match reservations by email
                .OrderByDescending(r => r.Start) // Show latest reservations first
                .ToListAsync();

            // Pass reservations to the view
            return View(reservations);
        }
    }
}
