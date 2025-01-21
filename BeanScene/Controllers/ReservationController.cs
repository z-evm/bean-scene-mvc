using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeanScene.Data;
using BeanScene.Models;
using Microsoft.AspNetCore.Identity;

namespace BeanScene.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationController(ApplicationDbContext context, ILogger<ReservationController> logger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult MakeReservation()
        {
            return View("Index");
        }

        // POST: /Reservation/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(DateTime date, TimeSpan time, int guests)
        {
            if (!ModelState.IsValid)
            {
                return View("Index");
            }

            DateTime selectedTime = date.Date.Add(time);
            DateTime rangeStart = selectedTime.AddHours(-1);
            DateTime rangeEnd = selectedTime.AddHours(1);

            var sittings = await _context.Sittings
                .Include(s => s.Reservations)
                .Where(s => s.Start <= rangeEnd && s.End >= rangeStart && !s.Closed)
                .ToListAsync();

            if (!sittings.Any())
            {
                ModelState.AddModelError("", "No sittings available within the specified range.");
                return View("Index");
            }

            var timeSlots = new List<(DateTime SlotStart, bool IsAvailable)>();

            foreach (var sitting in sittings)
            {
                DateTime currentTime = sitting.Start > rangeStart ? sitting.Start : rangeStart;
                while (currentTime < sitting.End && currentTime < rangeEnd)
                {
                    var overlappingReservations = sitting.Reservations?
                        .Where(r => r.End > currentTime && r.Start < currentTime.AddMinutes(15)) ?? Enumerable.Empty<Reservation>();

                    int totalReservedGuests = overlappingReservations.Sum(r => r.Pax);
                    bool isAvailable = totalReservedGuests + guests <= sitting.Capacity;

                    timeSlots.Add((currentTime, isAvailable));
                    currentTime = currentTime.AddMinutes(15);
                }
            }

            if (!timeSlots.Any())
            {
                ModelState.AddModelError("", "No time slots available within the specified range.");
                return View("Index");
            }

            ViewBag.TimeSlots = timeSlots;
            ViewBag.Guests = guests;
            ViewBag.SelectedDateTime = selectedTime;
            ViewBag.SittingId = sittings[0].Id;

            return View("Index");
        }

        // GET: /Reservation/Search
        [HttpGet]
        public IActionResult Search()
        {
            return View("Index");
        }

        // GET: /Reservation/Book
        public async Task<IActionResult> Book(int sittingId, int guests, DateTime selectedTimeSlot)
        {
            if (!ModelState.IsValid)
            {
                return View("Index");
            }

            Person? person = null;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                person = await _context.Persons.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                if (person == null)
                {
                    person = new Person
                    {
                        Name = currentUser.UserName ?? "",
                        Email = currentUser.Email ?? "",
                        Phone = currentUser.Email ?? ""
                    };

                    person.UserId = currentUser.Id;
                    _context.Persons.Add(person);
                    await _context.SaveChangesAsync();
                }
            }

            var sitting = await _context.Sittings
                .Include(s => s.Reservations)
                .FirstOrDefaultAsync(s => s.Id == sittingId && !s.Closed);

            if (sitting == null)
            {
                return NotFound("Sitting not available.");
            }

            ViewBag.SittingId = sittingId;
            ViewBag.Guests = guests;
            ViewBag.SelectedTimeSlot = selectedTimeSlot;

            return View("Index", new Reservation
            {
                Start = selectedTimeSlot,
                Pax = guests,
                Person = person!
            });
        }

        // POST: /Reservation/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Reservation reservation)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", reservation);
            }

            try
            {
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email!.ToLower() == reservation.Person.Email!.ToLower());
                if (person == null)
                {
                    person = new Person
                    {
                        Name = reservation.Person.Name,
                        Email = reservation.Person.Email,
                        Phone = reservation.Person.Phone
                    };
                    _context.Add(person);
                    await _context.SaveChangesAsync();
                }

                reservation.PersonId = person.Id;
                reservation.Person = null!;

                var sitting = await _context.Sittings
                    .Include(s => s.Reservations)
                    .FirstOrDefaultAsync(s => s.Id == reservation.SittingId);

                if (sitting == null || sitting.Closed)
                {
                    ModelState.AddModelError("", "Sitting not available.");
                    return View("Index", reservation);
                }

                var overlappingReservations = sitting.Reservations
                    .Where(r => r.Start < reservation.End && r.End > reservation.Start);
                var totalPax = overlappingReservations.Sum(r => r.Pax);

                if ((sitting.Capacity - totalPax) < reservation.Pax)
                {
                    ModelState.AddModelError("Pax", $"The maximum capacity for this sitting is {sitting.Capacity}. Only {sitting.Capacity - totalPax} spots are available.");
                    return View("Index", reservation);
                }

                DateTime end = reservation.Start.AddHours(2);

                var availableTables = await _context.RestaurantTables
                    .Include(t => t.Reservations)
                    .Where(t => t.Reservations.All(r => r.End <= reservation.Start || r.Start >= end))
                    .ToListAsync();

                if (!availableTables.Any())
                {
                    ModelState.AddModelError("", "No tables available for the selected time.");
                    return View("Index", reservation);
                }

                var random = new Random();
                var assignedTable = availableTables[random.Next(availableTables.Count)];

                reservation.Tables = new List<RestaurantTable> { assignedTable };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking reservation.");
                ModelState.AddModelError("", "Unexpected error occurred.");
                return View("Index", reservation);
            }
        }
    }
}
