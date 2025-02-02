using BeanScene.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeanScene.Areas.Staff.Controllers
{
    [Authorize(Roles = "Staff, Manager, Admin"), Area("Staff")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var selectedDate = date?.Date ?? DateTime.Today;
            var nextDay = selectedDate.AddDays(1);

            var reservations = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Sitting)
                .Include(r => r.ReservationStatus)
                .Include(r => r.Tables)
                .Where(r => r.Start >= selectedDate && r.Start < nextDay)
                .OrderBy(r => r.Start)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;

            return View(reservations);
        }
    }
}

