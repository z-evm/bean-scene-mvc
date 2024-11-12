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
                .Include(r => r.Person) //get person 
                .Include(r => r.Sitting)//get sitting time
                .Include(r =>r.ReservationStatus) //get reservation status
                .Include(r => r.Tables)// get reservation tables
                .ToListAsync();
            return View(reservations);
        }









       // GET: Reservation/Create
        public async Task<IActionResult>  Create()
        {
            ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
            return View();
        }


        //reservation create Working
        //post reservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( Reservation reservation)
        {
            

            try
            {
                if(ModelState.IsValid){

                
                // Find or create the person based on their email
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email == reservation.Person.Email);
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

                // Assign person to reservation
                reservation.PersonId = person.Id;

                // Find the specified sitting and ensure it’s open
                var sitting = await _context.Sittings
                                            .Include(s => s.Reservations)
                                            .FirstOrDefaultAsync(s => s.Id == reservation.SittingId);

                if (sitting == null || sitting.Closed)
                {
                    ModelState.AddModelError("SittingId", "Selected sitting is not available.");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                reservation.End = reservation.Start.AddMinutes(reservation.Duration);

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

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                // Handle all exceptions in a single catch block
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                Console.WriteLine($"Unexpected error: {ex.Message}");
                ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                
            }
            return View(reservation);
        }





        // POST: Reservation/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reservation reservation, string Name, string Email, string Phone)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                // Reload sittings in case of validation failure
                ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                return View(reservation);
            }

            try
            {
                // Find and update the associated Person details
                var person = await _context.Persons.FindAsync(reservation.PersonId);
                if (person != null)
                {
                    person.Name = Name;
                    person.Email = Email;
                    person.Phone = Phone;
                    _context.Update(person);
                }

                // Calculate the End time for the reservation
                reservation.End = reservation.Start.AddMinutes(reservation.Duration);

                // Update the reservation
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Directly check if the reservation exists in the database
                if (!_context.Reservations.Any(e => e.Id == reservation.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log the exception and add a generic error message
                Console.WriteLine($"Error updating reservation: {ex.Message}");
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            }

            // Reload sittings in case of error
            ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
            return View(reservation);
        }

                





            //delete is working
            // GET: Reservation/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)  // check reservation Id 
            {
                return NotFound();
            }

            var reservation = await _context.Reservations  // get reservation data 
                .Include(r => r.Person)
                .Include(r => r.Sitting)
                .Include(r =>r.Tables) //table
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
                _context.Reservations.Remove(reservation); //remove reservatin data 
                await _context.SaveChangesAsync(); // and save data 
            }
            return RedirectToAction(nameof(Index));
        }

        // Optional: Helper method to check if a reservation exists
        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }

    }
}
