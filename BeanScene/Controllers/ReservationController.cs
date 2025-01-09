using System;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeanScene.Data;
using BeanScene.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace BeanScene.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        // Constructor to initialize the controller with dependencies
        public ReservationController(ApplicationDbContext context, ILogger<ReservationController> logger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: /Reservation/Search
        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        // POST: /Reservation/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(DateTime date, TimeSpan time, int guests)
        {
            if (!ModelState.IsValid)
            {
                return View("Search");
            }

            // Calculate the selected time and the range for searching sittings
            DateTime selectedTime = date.Date.Add(time);
            DateTime rangeStart = selectedTime.AddHours(-1);
            DateTime rangeEnd = selectedTime.AddHours(1);

            // Find sittings within the specified range that are not closed
            var sittings = await _context.Sittings 
                .Include(s => s.Reservations)
                .Where(s => s.Start <= rangeEnd && s.End >= rangeStart && !s.Closed)
                .ToListAsync();

            if (!sittings.Any())
            {
                ModelState.AddModelError("", "No sittings available within the specified range.");
                return View("Search");
            }

            var timeSlots = new List<(DateTime SlotStart, bool IsAvailable)>();

            // Check availability of time slots within the sittings
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
                return View("Search");
            }

            // Pass the available time slots to the view
            ViewBag.TimeSlots = timeSlots; 
            ViewBag.Guests = guests;
            ViewBag.SelectedDateTime = selectedTime;
            ViewBag.SittingId = sittings[0].Id;

            return View("TimeSlotList");
        }

        // GET: /Reservation/Book
        public async Task<IActionResult> Book(int sittingId, int guests, DateTime selectedTimeSlot)
        {
            if (!ModelState.IsValid)
            {
                return View("Book");
            }

            Person? person = null;

            // Get the current user and find or create a corresponding Person entity
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
                        Phone = currentUser.Email ??"" 
                    };

                    person.UserId = currentUser.Id;
                    _context.Persons.Add(person);
                    await _context.SaveChangesAsync();
                }
            }

            // Find the sitting by ID and ensure it is not closed
            var sitting = await _context.Sittings
                .Include(s => s.Reservations)
                .FirstOrDefaultAsync(s => s.Id == sittingId && !s.Closed);

            if (sitting == null)
            {
                return NotFound("Sitting not available.");
            }

            // Pass the booking details to the view
            ViewBag.SittingId = sittingId;
            ViewBag.Guests = guests;
            ViewBag.SelectedTimeSlot = selectedTimeSlot;

            return View(new Reservation
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
                return View(reservation);
            }

            try
            {
                // Find or create the person based on the provided email
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

                // Validate sitting
                var sitting = await _context.Sittings
                    .Include(s => s.Reservations)
                    .FirstOrDefaultAsync(s => s.Id == reservation.SittingId);

                if (sitting == null || sitting.Closed)
                {
                    ModelState.AddModelError("", "Sitting not available.");
                    return View(reservation);
                }

                // Validate capacity
                var overlappingReservations = sitting.Reservations
                    .Where(r => r.Start < reservation.End && r.End > reservation.Start);
                var totalPax = overlappingReservations.Sum(r => r.Pax);

                if ((sitting.Capacity - totalPax) < reservation.Pax)
                {
                    ModelState.AddModelError("Pax", $"The maximum capacity for this sitting is {sitting.Capacity}. Only {sitting.Capacity - totalPax} spots are available.");
                    return View(reservation);
                }

                // Calculate the end time of the reservation (2 hours from the start)
                DateTime end = reservation.Start.AddHours(2);

                // Fetch all available tables for the given time slot
                var availableTables = await _context.RestaurantTables
                    .Include(t => t.Reservations) // Include reservations for the availability check
                    .Where(t => t.Reservations.All(r => r.End <= reservation.Start || r.Start >= end)) // No overlapping reservations
                    .ToListAsync();

                if (!availableTables.Any())
                {
                    ModelState.AddModelError("", "No tables available for the selected time.");
                    return View(reservation);
                }

                // Select one table (random or best-fit) from the available tables
                var random = new Random();
                var assignedTable = availableTables[random.Next(availableTables.Count)];

                // Assign the selected table to the reservation
                reservation.Tables = new List<RestaurantTable> { assignedTable };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking reservation.");
                ModelState.AddModelError("", "Unexpected error occurred.");
                return View(reservation);
            }
        }
    }
}