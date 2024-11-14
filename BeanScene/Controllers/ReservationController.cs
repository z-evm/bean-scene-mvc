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
            var reservations = await _context.Reservations // fetch reservation data
                .Include(r => r.Person) //get person 
                .Include(r => r.Sitting)//get sitting time
                .Include(r =>r.ReservationStatus) //get reservation status
                .Include(r => r.Tables)// get reservation tables
                .ToListAsync();
            return View(reservations);
        }
















        //reservation create Working
       // GET: Reservation/Create
        public async Task<IActionResult>  Create()
        {

            // Fetch only upcoming sittings that are open and sort by start time
                 ViewData["Sittings"] = await _context.Sittings
                                         .Where(s => !s.Closed && s.Start >= DateTime.Now) // this turn
                                         .OrderBy(s => s.Start)
                                         .ToListAsync();
            return View();
        }




        //reservation create Working
        //post reservation
        [HttpPost]
        [ValidateAntiForgeryToken] // create token
        public async Task<IActionResult> Create( Reservation reservation)
        {
            

            try
            {
                if(ModelState.IsValid){

                
                // Find or create the person based on their email
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email!.ToLower() == reservation.Person.Email!.ToLower());  // check email
                if (person == null)
                {
                    person = new Person  //create new person
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

                reservation.Person = null; // solved new user problem

                

                // Find the specified sitting and ensure it’s open
                var sitting = await _context.Sittings
                                            .Include(s => s.Reservations)
                                            .FirstOrDefaultAsync(s => s.Id == reservation.SittingId);

                if (sitting == null || sitting.Closed)
                {
                    ModelState.AddModelError("SittingId", "Selected sitting is not available.");  // if is not 
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                reservation.End = reservation.Start.AddMinutes(reservation.Duration);


                // Ensure the reservation is within the sitting's start and end times
                if (reservation.Start < sitting.Start || reservation.End > sitting.End)
                {
                    ModelState.AddModelError("Start", "Reservation must fit within the sitting's start and end times.");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed && s.Start >= DateTime.Now).OrderBy(s => s.Start).ToListAsync();
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





















        // GET: Reservation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Load the reservation along with related Person and Sitting entities
            var reservation = await _context.Reservations
                .Include(r => r.Person) // Make sure Person is loaded
                .Include(r => r.Sitting) // Make sure Sitting is loaded
                .Include(r =>r.Tables)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Load sittings for the dropdown
            ViewData["Sittings"] = await _context.Sittings
                .Where(s => !s.Closed) // Filter for open sittings
                .ToListAsync();

            return View(reservation);
        }





    // POST: Reservation/Edit/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Reservation reservation)
    {
        if (id != reservation.Id)
        {
            return BadRequest("Reservation ID mismatch.");
        }

        try
        {
            if (ModelState.IsValid)
            {
                // Retrieve the original reservation including the related person, sitting, and tables data
                var originalReservation = await _context.Reservations
                                                        .Include(r => r.Person)
                                                        .Include(r => r.Sitting)
                                                        .Include(r => r.Tables) // Corrected typo here
                                                        .FirstOrDefaultAsync(r => r.Id == id);

                if (originalReservation == null)
                {
                    return NotFound("Reservation not found.");
                }

                // Find or create the person based on their email
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email!.ToLower() == reservation.Person.Email!.ToLower());
                if (person == null)
                {
                    // New person creation
                    person = new Person
                    {
                        Name = reservation.Person.Name,
                        Email = reservation.Person.Email,
                        Phone = reservation.Person.Phone
                    };
                    _context.Add(person);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update person's details if they have changed
                    person.Name = reservation.Person.Name;
                    person.Phone = reservation.Person.Phone;
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                }

                // Update the reservation's person ID
                originalReservation.PersonId = person.Id;

                // Calculate the end time of the reservation
                reservation.End = reservation.Start.AddMinutes(reservation.Duration);

                // Ensure reservation time is within the sitting's start and end times
                var sitting = await _context.Sittings
                                            .FirstOrDefaultAsync(s => s.Id == originalReservation.SittingId && !s.Closed);
                if (sitting == null || reservation.Start < sitting.Start || reservation.End > sitting.End)
                {
                    ModelState.AddModelError("Start", "Reservation time must fit within the sitting's available time.");
                    ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
                    return View(reservation);
                }

                // Update reservation details
                originalReservation.Start = reservation.Start;
                originalReservation.End = reservation.End;
                originalReservation.Duration = reservation.Duration;

                // Update the reservation
                _context.Update(originalReservation);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency issues
            if (!_context.Reservations.Any(e => e.Id == reservation.Id))
            {
                return NotFound("Reservation does not exist.");
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            // Log the exception and show a generic error message
            Console.WriteLine($"Error updating reservation: {ex.Message}");
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
        }

        // Reload sittings in case of an error to repopulate dropdowns
        ViewData["Sittings"] = await _context.Sittings.Where(s => !s.Closed).ToListAsync();
        return View(reservation);
    }
    

    










            
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
                _context.Reservations.Remove(reservation); //remove reservation data 
                await _context.SaveChangesAsync(); // and save data 
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
