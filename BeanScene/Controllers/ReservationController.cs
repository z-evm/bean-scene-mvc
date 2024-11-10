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

        // GET: Reservation/Index
        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Sitting)
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

        // POST: Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Start,Duration,Pax,Notes,SittingId")] Reservation reservation, string Name, string Email, string Phone)
        {
            if (ModelState.IsValid)
            {
                // Find or create the person based on their email
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email == Email);
                if (person == null)
                {
                    person = new Person
                    {
                        Name = Name,
                        Email = Email,
                        Phone = Phone
                    };
                    _context.Add(person);
                    await _context.SaveChangesAsync();
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
                var availableTables = await _context.RestaurantTables
                    .Include(t => t.Reservations)
                    .Where(t => t.Reservations.All(r => r.End <= reservation.Start || r.Start >= reservation.End))
                    .ToListAsync();

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
                    Console.WriteLine("Error saving reservation: " + ex.Message);
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
            return View(reservation);
        }

        // GET: Reservation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
            return View(reservation);
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
