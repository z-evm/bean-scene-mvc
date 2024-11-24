using BeanScene.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeanScene.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeanScene.Areas.Staff.Controllers
{
    [Authorize(Roles = "Staff,Admin"), Area("Staff")]
    public class StaffAreaController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<StaffAreaController> _logger;

        public StaffAreaController(ApplicationDbContext context ,ILogger<StaffAreaController> logger)
        {
            _context = context;
            _logger = logger;
        }







        [HttpGet]
public async Task<IActionResult> Index(DateTime? date)
{
    // Default to the current date if no date is provided
    var selectedDate = date?.Date ?? DateTime.Today;
    var nextDay = selectedDate.AddDays(1);

    // Fetch reservations for the selected date
    var reservations = await _context.Reservations
        .Include(r => r.Person)
        .Include(r => r.Sitting)
        .Include(r => r.ReservationStatus)
        .Include(r => r.Tables)
        .Where(r => r.Start >= selectedDate && r.Start < nextDay) // Filter by selected date
        .OrderBy(r => r.Start) // Order by start time
        .ToListAsync();

    // Pass the selected date to the view
    ViewBag.SelectedDate = selectedDate;

    return View(reservations);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleStatus(int id)
{
    var reservation = await _context.Reservations
        .Include(r => r.ReservationStatus)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reservation == null)
    {
        return NotFound("Reservation not found.");
    }

    // Define constants for ReservationStatus IDs
    const int PendingStatusId = 1;  // Example: Pending
    const int ConfirmedStatusId = 2;  // Example: Approved
    const int SeatedStatusId = 3;  // Example: Seated
    const int FinishedStatusId = 4;  // Example: Finished

    // Handle status changes based on the current status
    switch (reservation.ReservationStatusId)
    {
        case PendingStatusId:
            // Allow changing to Approved anytime
            reservation.ReservationStatusId = ConfirmedStatusId;
            break;

        case ConfirmedStatusId:
            // Allow changing to Seated only if Start time has passed
            if (reservation.Start > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Cannot mark as Seated before the reservation start time.";
                return RedirectToAction("Index", new { date = reservation.Start.Date });
            }
            reservation.ReservationStatusId = SeatedStatusId;
            break;

        case SeatedStatusId:
            // Allow changing to Finished only if Start time has passed
            if (reservation.Start > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Cannot mark as Finished before the reservation start time.";
                return RedirectToAction("Index", new { date = reservation.Start.Date });
            }
            reservation.ReservationStatusId = FinishedStatusId;

            // Set the End time and calculate the duration
            reservation.End = DateTime.Now;
            if (reservation.Start != null && reservation.End != null)
            {
                reservation.Duration = (int)(reservation.End.Value - reservation.Start).TotalMinutes; // Duration in minutes
            }
            break;

        default:
            TempData["ErrorMessage"] = "Invalid reservation status transition.";
            return RedirectToAction("Index", new { date = reservation.Start.Date });
    }

    // Update the reservation status
    _context.Update(reservation);
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "Reservation status updated successfully.";
    return RedirectToAction("Index", new { date = reservation.Start.Date });
}








[HttpGet]
public async Task<IActionResult> Details(int id)
{
    // Fetch the reservation by ID with related data
    var reservation = await _context.Reservations
        .Include(r => r.Person)          // Include the person who made the reservation
        .Include(r => r.Tables)          // Include the associated tables
        .Include(r => r.ReservationStatus) // Include the reservation status
        .Include(r => r.Sitting)         // Include the sitting
        .FirstOrDefaultAsync(r => r.Id == id);

    // If reservation is not found, return a 404 error
    if (reservation == null)
    {
        return NotFound("Reservation not found.");
    }

    ViewBag.ReservationId = id;


    // Pass the reservation to the view
    return View(reservation);
}












      [HttpGet]
        public IActionResult Search()
        {
            return View();
        }



        // Post /Reservation/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(DateTime date, TimeSpan time, int guests)
        {
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
                return View("Search");
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
                return View("Search");
            }

            ViewBag.TimeSlots = timeSlots;
            ViewBag.Guests = guests;
            ViewBag.SelectedDateTime = selectedTime;
            ViewBag.SittingId = sittings.First().Id;

            return View("TimeSlotList");
        }



// GET
public async Task<IActionResult> Book(int sittingId, int guests, DateTime selectedTimeSlot)
{
    // Fetch all sittings that could match the selected time
    var sittings = await _context.Sittings
        .Include(s => s.Reservations)
        .Where(s => !s.Closed && s.Start <= selectedTimeSlot && s.End >= selectedTimeSlot)
        .ToListAsync();

    // Find the sitting matching the time slot
    var sitting = sittings.FirstOrDefault(s => s.Id == sittingId) ?? sittings.FirstOrDefault();

    if (sitting == null)
    {
        TempData["ErrorMessage"] = "No suitable sitting available for the selected time.";
        return RedirectToAction("Index"); // Redirect to an appropriate view
    }

    // Calculate the reservation's time range
    var reservationEndTime = selectedTimeSlot.AddMinutes(120); // Default reservation duration is 2 hours

    // Fetch tables that are available for the entire reservation duration
    var availableTables = await _context.RestaurantTables
        .Include(t => t.Reservations)
        .Where(t => t.Reservations.All(r =>
            r.End <= selectedTimeSlot || r.Start >= reservationEndTime)) // Check no overlapping reservations
        .ToListAsync();

    // Update ViewBag values for the sitting and other details
    ViewBag.SittingId = sitting.Id; // Update sittingId dynamically
    ViewBag.Guests = guests;
    ViewBag.SelectedTimeSlot = selectedTimeSlot;
    ViewBag.AvailableTables = availableTables;

    return View(new Reservation
    {
        Start = selectedTimeSlot,
        Pax = guests,
        Sitting = sitting
    });
}




        // POST: Reservation/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Reservation reservation,int[] selectedTableIds)
        {
            if (!ModelState.IsValid)
            {
                return View(reservation);
            }

            try
            {   //1 . Find or create the person based on their email
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
                    return View(reservation);
                }

                var selectedTables = await _context.RestaurantTables
                .Where(t => selectedTableIds.Contains(t.Id))
                .ToListAsync();

                 if (!selectedTables.Any() || selectedTables.Count != selectedTableIds.Length)
                {
                    ModelState.AddModelError("selectedTableIds", "One or more selected tables are invalid.");
                    return View(reservation);
                }

                reservation.Tables = selectedTables;

                  


                //3. calculate pax 
                // Calculate total Pax from overlapping reservations
                var totalPax = sitting.Reservations
                        .Where(r => r.Start < reservation.End && r.End > reservation.Start) // Overlapping reservations
                        .Sum(r => r.Pax);

                // Reduce capacity dynamically
                var availableCapacity = sitting.Capacity - totalPax;


               

                // 4. Validate if there is enough capacity
                if (availableCapacity < reservation.Pax)
                {
                    ModelState.AddModelError("Pax", $"The maximum capacity for this sitting is {sitting.Capacity}. Only {availableCapacity} spots are available.");
                    return View(reservation);
                }


                reservation.End = reservation.Start.AddMinutes(120); 

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Unexpected error occurred.");
                _logger.LogError(ex, "Error booking reservation.");
                return View(reservation);
            }
        }











 //Edit Controler

[HttpGet]
public async Task<IActionResult> EditSearch(int id)
{
    // Fetch the reservation data
    var reservation = await _context.Reservations
        .Include(r => r.Person)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reservation == null)
    {
        return NotFound("Reservation not found.");
    }

    // Pass reservation data to the view
    ViewBag.ReservationId = reservation.Id;
    ViewBag.Date = reservation.Start.Date.ToString("yyyy-MM-dd"); // Format as required for HTML date input
    ViewBag.Time = reservation.Start.ToString("HH:mm"); // Format as required for HTML time input
    ViewBag.Guests = reservation.Pax;

    return View();
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditSearch(int id, DateTime date, TimeSpan time, int guests)
{
    // Fetch reservation details by ID
    var reservation = await _context.Reservations
        .Include(r => r.Person)
        .FirstOrDefaultAsync(r => r.Id == id);

    

    if (reservation == null)
    {
        return NotFound("Reservation not found.");
    }

    DateTime selectedTime = date.Date.Add(time);

    // Fetch available time slots (1 hour before and after selected time)
    DateTime rangeStart = selectedTime.AddHours(-1);
    DateTime rangeEnd = selectedTime.AddHours(1);

    var sittings = await _context.Sittings
        .Include(s => s.Reservations)
        .Where(s => s.Start <= rangeEnd && s.End >= rangeStart && !s.Closed)
        .ToListAsync();

    if (!sittings.Any())
    {
        ModelState.AddModelError("", "No sittings available within the specified range.");
        return View("EditSearch");
    }

    // Assign the appropriate SittingId
    var sitting = sittings.FirstOrDefault(s => s.Start <= selectedTime && s.End >= selectedTime);

    if (sitting == null)
    {
        ModelState.AddModelError("", "No sitting available for the selected time.");
        return View("EditSearch");
    }

    // Pass SittingId to the view

    var timeSlots = new List<(DateTime SlotStart, bool IsAvailable)>();
    foreach (var currentSitting in sittings)
    {
        DateTime currentTime = currentSitting.Start > rangeStart ? currentSitting.Start : rangeStart;
        while (currentTime < sitting.End && currentTime < rangeEnd)
        {
            var overlappingReservations = currentSitting.Reservations?
                        .Where(r => r.End > currentTime && r.Start < currentTime.AddMinutes(15)) ?? Enumerable.Empty<Reservation>();

            int totalReservedGuests = overlappingReservations.Sum(r => r.Pax);
            bool isAvailable = totalReservedGuests + guests <= currentSitting.Capacity;

            timeSlots.Add((currentTime, isAvailable));
            currentTime = currentTime.AddMinutes(15);
        }
    }

    if (!timeSlots.Any())
    {
        ModelState.AddModelError("", "No time slots available within the specified range.");
        return View("EditSearch");
    }
    ViewBag.SittingId = sitting.Id; 
    ViewBag.TimeSlots = timeSlots;
    ViewBag.ReservationId = reservation.Id;
    ViewBag.Guests = guests;
    ViewBag.SelectedDateTime = selectedTime;

    return View("EditTimeSlotList", reservation); // Show available time slots
}





[HttpGet]
public async Task<IActionResult> EditBook(int id, int sittingId, int guests, DateTime selectedTimeSlot)
{
    _logger.LogInformation("EditBook called with id={Id}, sittingId={SittingId}, guests={Guests}, selectedTimeSlot={SelectedTimeSlot}",
        id, sittingId, guests, selectedTimeSlot);

    // Fetch the reservation with its associated tables
    var reservation = await _context.Reservations
        .Include(r => r.Person) // Include the person associated with the reservation
        .Include(r => r.Tables) // Include the tables associated with the reservation
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reservation == null)
    {
        return NotFound("Reservation not found.");
    }

   // Calculate reservation end time
    var reservationEndTime = selectedTimeSlot.AddMinutes(120);

    // Fetch tables available for the current time slot (exclude overlapping ones)
    var availableTables = await _context.RestaurantTables
        .Include(t => t.Reservations)
        .Where(t => !t.Reservations.Any(r =>
            r.Start < reservationEndTime && r.End > selectedTimeSlot && r.Id != id)) // Exclude overlapping reservations not related to this reservation
        .ToListAsync();


    // Pass the necessary data to the view
    ViewBag.Reservation = reservation;
    ViewBag.AvailableTables = availableTables;
    ViewBag.SelectedTimeSlot = selectedTimeSlot;
    ViewBag.Guests = guests;
    ViewBag.SittingId = sittingId;

    return View(reservation);
}











[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditBook(Reservation reservation, int[] selectedTableIds)
{
    if (!ModelState.IsValid)
    {
        _logger.LogWarning("ModelState is invalid: {Errors}", ModelState.Values.SelectMany(v => v.Errors));
        return View(reservation);
    }

    var originalReservation = await _context.Reservations
        .Include(r => r.Person)
        .Include(r => r.Tables)
        .FirstOrDefaultAsync(r => r.Id == reservation.Id);

    if (originalReservation == null)
    {
        return NotFound("Reservation not found.");
    }

    try
    {
        // Validate person details
        if (reservation.Person == null || string.IsNullOrWhiteSpace(reservation.Person.Email))
        {
            ModelState.AddModelError("", "Person details are required.");
            return View(reservation);
        }

        // Find or create the person
        var person = await _context.Persons.FirstOrDefaultAsync(p => p.Email!.ToLower() == reservation.Person.Email.ToLower());
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

        // Update reservation details
        originalReservation.PersonId = person.Id;
        originalReservation.Start = reservation.Start;
        originalReservation.End = reservation.Start.AddMinutes(120); // Adjust end time logic if needed
        originalReservation.Pax = reservation.Pax;
         originalReservation.Notes = reservation.Notes;

        // Update associated tables
        if (selectedTableIds != null && selectedTableIds.Any())
        {
            var selectedTables = await _context.RestaurantTables
                .Where(t => selectedTableIds.Contains(t.Id))
                .ToListAsync();

            originalReservation.Tables = selectedTables;
        }
        else
        {
            originalReservation.Tables = new List<RestaurantTable>(); // Clear tables if none are selected
        }

        // Save changes
        _context.Update(originalReservation);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reservation updated successfully.";
        return RedirectToAction("Index", "StaffArea");
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", "Unexpected error occurred.");
        _logger.LogError(ex, "Error updating reservation.");
        return View(reservation);
    }
}








    //DELETE CONTROLLER 


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
                .Include(r => r.Tables) //table
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
            var reservation = await _context.Reservations
                .Include(r => r.Sitting) // Include the associated Sitting entity
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reservation != null)
            {
                var sitting = reservation.Sitting;
               

                _context.Reservations.Remove(reservation); //remove reservation data 
                await _context.SaveChangesAsync(); // and save data 
            }
            return RedirectToAction(nameof(Index));
        }




    }
}




