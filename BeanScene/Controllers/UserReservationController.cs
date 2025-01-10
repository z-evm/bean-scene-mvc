using BeanScene.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeanScene.Controllers
{
    public class UserReservationController : Controller
    {
        private readonly ILogger<UserReservationController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Constructor to initialize the controller with dependencies
        public UserReservationController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<UserReservationController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // GET: /UserReservation/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Index action called.");

            // Get the currently logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Redirect to login if the user is not authenticated
                return RedirectToAction("Login", "Account");
            }

            // Get the user's email
            var userEmail = user.Email;

            // Fetch reservations associated with the user's email
            var reservations = await _context.Reservations
                .Include(r => r.Sitting) // Include related Sitting entity
                .Include(r => r.Person) // Include related Person entity
                .Where(r => r.Person.Email == userEmail) // Filter by user's email
                .OrderByDescending(r => r.Start) // Order by start date descending
                .ToListAsync();

            // Return the view with the list of reservations
            return View(reservations);
        }
    }
}
