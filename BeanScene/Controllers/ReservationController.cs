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


        public ReservationController(ApplicationDbContext context, ILogger<ReservationController> logger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // Get /Reservation/Search
        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }





        // Post /Reservation/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(DateTime date, TimeSpan time, int guests) // get param form search screen  push to listOftimeslot screen
        {
            DateTime selectedTime = date.Date.Add(time); // set up request  time
            DateTime rangeStart = selectedTime.AddHours(-1); //  reservation search start time , 
            DateTime rangeEnd = selectedTime.AddHours(1); //reservation search end time , 

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

            foreach (var sitting in sittings)
            {

                DateTime currentTime = sitting.Start > rangeStart ? sitting.Start : rangeStart;  // get available time 
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

            ViewBag.TimeSlots = timeSlots; 
            ViewBag.Guests = guests;
            ViewBag.SelectedDateTime = selectedTime;
            ViewBag.SittingId = sittings.First().Id;

            return View("TimeSlotList"); // to TimeSlotList
        }



        // GET: Reservation/Book
        public async Task<IActionResult> Book(int sittingId, int guests, DateTime selectedTimeSlot)
        {
        Person? person = null; //get person

        // Check if a user is logged in
        var currentUser = await _userManager.GetUserAsync(User); // if it is login
        if (currentUser != null)
        {
            // Find or create the associated Person
            person = await _context.Persons.FirstOrDefaultAsync(p => p.UserId == currentUser.Id); // this assoite with person and user
            if (person == null)
            {
                // Create a new Person object to prefill with logged-in user's details
                person = new Person
                {
                    Name = currentUser.UserName ?? "",
                    Email = currentUser.Email ?? "",
                    Phone = currentUser.Email ??"" 
                };

                // Save the new Person in the database and associate it with the current user
                person.UserId = currentUser.Id;
                _context.Persons.Add(person);
                await _context.SaveChangesAsync();
            }
        }

        // Fetch the Sitting
        var sitting = await _context.Sittings
            .Include(s => s.Reservations)
            .FirstOrDefaultAsync(s => s.Id == sittingId && !s.Closed);

        if (sitting == null)
        {
            return NotFound("Sitting not available.");
        }

        // Populate ViewBag for Razor rendering
        ViewBag.SittingId = sittingId;
        ViewBag.Guests = guests;
        ViewBag.SelectedTimeSlot = selectedTimeSlot;

        // Return the reservation form
        return View(new Reservation
        {
            Start = selectedTimeSlot,
            Pax = guests,
            Person = person! // Pre-fill for logged-in users, or null for guests
        });
    }






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

