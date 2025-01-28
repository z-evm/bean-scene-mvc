using BeanScene.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeanScene.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeanScene.Areas.Staff.Controllers
{
    [Authorize(Roles = "Staff, Manager, Admin"), Area("Staff")]
    public class StaffAreaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffAreaController> _logger;

        public StaffAreaController(ApplicationDbContext context, ILogger<StaffAreaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Reservation(DateTime? date)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var reservation = await _context.Reservations
                .Include(r => r.ReservationStatus)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
            }

            const int PendingStatusId = 1;
            const int ConfirmedStatusId = 2;
            const int SeatedStatusId = 3;
            const int FinishedStatusId = 4;

            switch (reservation.ReservationStatusId)
            {
                case PendingStatusId:
                    reservation.ReservationStatusId = ConfirmedStatusId;
                    break;

                case ConfirmedStatusId:
                    if (reservation.Start > DateTime.Now)
                    {
                        TempData["ErrorMessage"] = "Cannot mark as Seated before the reservation start time.";
                        return RedirectToAction("Index", new { date = reservation.Start.Date });
                    }
                    reservation.ReservationStatusId = SeatedStatusId;
                    break;

                case SeatedStatusId:
                    if (reservation.Start > DateTime.Now)
                    {
                        TempData["ErrorMessage"] = "Cannot mark as Finished before the reservation start time.";
                        return RedirectToAction("Index", new { date = reservation.Start.Date });
                    }
                    reservation.ReservationStatusId = FinishedStatusId;
                    reservation.End = DateTime.Now;
                    if (reservation.End != null)
                    {
                        reservation.Duration = (int)(reservation.End.Value - reservation.Start).TotalMinutes;
                    }
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid reservation status transition.";
                    return RedirectToAction("Index", new { date = reservation.Start.Date });
            }

            _context.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation status updated successfully.";
            return RedirectToAction("Index", new { date = reservation.Start.Date });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Tables)
                .Include(r => r.ReservationStatus)
                .Include(r => r.Sitting)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
            }

            ViewBag.ReservationId = id;

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(DateTime date, TimeSpan time, int guests)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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
            ViewBag.SittingId = sittings[0].Id;

            return View("TimeSlotList");
        }

        [HttpGet]
        public async Task<IActionResult> Book(int sittingId, int guests, DateTime selectedTimeSlot)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sittings = await _context.Sittings
                .Include(s => s.Reservations)
                .Where(s => !s.Closed && s.Start <= selectedTimeSlot && s.End >= selectedTimeSlot)
                .ToListAsync();

            var sitting = sittings.FirstOrDefault(s => s.Id == sittingId) ?? sittings.FirstOrDefault();

            if (sitting == null)
            {
                TempData["ErrorMessage"] = "No suitable sitting available for the selected time.";
                return RedirectToAction("Index");
            }

            var reservationEndTime = selectedTimeSlot.AddMinutes(120);

            var availableTables = await _context.RestaurantTables
                .Include(t => t.Reservations)
                .Where(t => t.Reservations.All(r =>
                    r.End <= selectedTimeSlot || r.Start >= reservationEndTime))
                .ToListAsync();

            ViewBag.SittingId = sitting.Id;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Reservation reservation, int[] selectedTableIds)
        {
            if (!ModelState.IsValid)
            {
                return View(reservation);
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

                var totalPax = sitting.Reservations
                    .Where(r => r.Start < reservation.End && r.End > reservation.Start)
                    .Sum(r => r.Pax);

                var availableCapacity = sitting.Capacity - totalPax;

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

        [HttpGet]
        public async Task<IActionResult> EditSearch(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
            }

            ViewBag.ReservationId = reservation.Id;
            ViewBag.Date = reservation.Start.Date.ToString("yyyy-MM-dd");
            ViewBag.Time = reservation.Start.ToString("HH:mm");
            ViewBag.Guests = reservation.Pax;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSearch(int id, DateTime date, TimeSpan time, int guests)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
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
                return View("EditSearch");
            }

            var sitting = sittings.FirstOrDefault(s => s.Start <= selectedTime && s.End >= selectedTime);

            if (sitting == null)
            {
                ModelState.AddModelError("", "No sitting available for the selected time.");
                return View("EditSearch");
            }

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

            return View("EditTimeSlotList", reservation);
        }

        [HttpGet]
        public async Task<IActionResult> EditBook(int id, int sittingId, int guests, DateTime selectedTimeSlot)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("EditBook called with id={Id}, sittingId={SittingId}, guests={Guests}, selectedTimeSlot={SelectedTimeSlot}",
                id, sittingId, guests, selectedTimeSlot);

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
            }

            var reservationEndTime = selectedTimeSlot.AddMinutes(120);

            var availableTables = await _context.RestaurantTables
                .Include(t => t.Reservations)
                .Where(t => !t.Reservations.Any(r =>
                    r.Start < reservationEndTime && r.End > selectedTimeSlot && r.Id != id))
                .ToListAsync();

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
                if (reservation.Person == null || string.IsNullOrWhiteSpace(reservation.Person.Email))
                {
                    ModelState.AddModelError("", "Person details are required.");
                    return View(reservation);
                }

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

                originalReservation.PersonId = person.Id;
                originalReservation.Start = reservation.Start;
                originalReservation.End = reservation.Start.AddMinutes(120);
                originalReservation.Pax = reservation.Pax;
                originalReservation.Notes = reservation.Notes;

                if (selectedTableIds != null && selectedTableIds.Any())
                {
                    var selectedTables = await _context.RestaurantTables
                        .Where(t => selectedTableIds.Contains(t.Id))
                        .ToListAsync();

                    originalReservation.Tables = selectedTables;
                }
                else
                {
                    originalReservation.Tables = new List<RestaurantTable>();
                }

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

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Person)
                .Include(r => r.Sitting)
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var reservation = await _context.Reservations
                .Include(r => r.Sitting)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
