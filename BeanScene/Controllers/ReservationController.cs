using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeanScene.Data;
using BeanScene.Models;

namespace BeanScene.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }


        //This is working
        // GET: Reservation/Index
        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Sitting)
                .Include(r =>r.ReservationStatus)
                .Include(r => r.Tables)
                .ToListAsync();
            return View(reservations);
        }





        // GET: Reservation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Sitting)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }






       // GET: Reservation/Create
        public async Task<IActionResult>  Create()
        {
            ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Start,Duration,Pax,Notes,SittingId,Person.Name,Person.Email,Person.Phone")] Reservation reservation)
        {
            if (!ModelState.IsValid)
            {
                // Log all validation errors to the console for debugging
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Error: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }

                // Reload sittings and return the view with validation messages
                ModelState.AddModelError("", "Please correct the highlighted errors.");
                ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                return View(reservation);
            }

            try
            {
                // Find or create the person based on their email
               var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email == reservation.Person.Email);
                if (person == null)
                {
                    // If person is not found, create a new person using the details from reservation.Person
                    person = new Person
                    {
                        Name = reservation.Person.Name,
                        Email = reservation.Person.Email,
                        Phone = reservation.Person.Phone
                    };


                    try
                    {
                        _context.Add(person);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "There was an error saving the person details. Please try again.");
                        Console.WriteLine($"Error saving person to database: {ex.Message}");
                        ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                        return View(reservation);
                    }
                }

                // Assign person to reservation
                reservation.PersonId = person.Id;

                // Find the specified sitting and ensure it’s open
                var sitting = await _context.Sittings.Include(s => s.Reservations)
                                                    .FirstOrDefaultAsync(s => s.Id == reservation.SittingId);

                if (sitting == null || sitting.Closed)
                {
                    ModelState.AddModelError("SittingId", "Selected sitting is not available.");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                // Get a list of available tables for the selected time
                List<RestaurantTable> availableTables;
                try
                {
                    availableTables = await _context.RestaurantTables
                        .Include(t => t.Reservations)
                        .Where(t => t.Reservations.All(r => r.End <= reservation.Start || r.Start >= reservation.End))
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "There was an error fetching available tables. Please try again.");
                    Console.WriteLine($"Error retrieving available tables: {ex.Message}");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                if (!availableTables.Any())
                {
                    ModelState.AddModelError("", "No available table for the selected time.");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                // Randomly select a table from the list of available tables
                var random = new Random();
                var randomTable = availableTables[random.Next(availableTables.Count)];

                // Assign table to reservation and complete setup
                reservation.Tables.Add(randomTable);
                _context.Add(reservation);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "There was an error saving the reservation. Please try again.");
                    Console.WriteLine($"Error saving reservation to database: {ex.Message}");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                Console.WriteLine($"Unexpected error: {ex.Message}");
                ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                return View(reservation);
            }
        }





        // POST: Reservation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Start,Duration,Pax,Notes,SittingId")] Reservation reservation, string Name, string Email, string Phone)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update person details based on provided Name, Email, and Phone
                var person = await _context.Persons.FindAsync(reservation.PersonId);
                if (person != null)
                {
                    person.Name = Name;
                    person.Email = Email;
                    person.Phone = Phone;
                    _context.Update(person);
                }

                // Update reservation
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
            return View(reservation);
        }



        // GET: Reservation/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Sitting)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}
