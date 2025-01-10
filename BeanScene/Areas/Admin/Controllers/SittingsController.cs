using BeanScene.Data;
using BeanScene.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeanScene.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin"), Area("Admin")]
    public class SittingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SittingsController> _logger;

        public SittingsController(ILogger<SittingsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Index action called with date: {Date}", date);

            var selectedDate = date?.Date ?? DateTime.Today;
            var nextDay = selectedDate.AddDays(1);

            try
            {
                var sittings = await _context.Sittings
                    .Include(s => s.Restaurant)
                    .Include(s => s.Reservations)
                    .Where(s => s.Start >= selectedDate && s.Start < nextDay)
                    .OrderBy(s => s.Start)
                    .ToListAsync();

                var currentTime = DateTime.Now;
                foreach (var sitting in sittings)
                {
                    if (sitting.End < currentTime && !sitting.Closed)
                    {
                        sitting.Closed = true;
                        _context.Update(sitting);
                    }
                }

                await _context.SaveChangesAsync();
                ViewBag.SelectedDate = selectedDate;

                return View(sittings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching sittings for date: {Date}", date);
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                _logger.LogWarning("Details action called with null id");
                return NotFound();
            }

            try
            {
                var sitting = await _context.Sittings
                    .Include(s => s.Reservations)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (sitting == null)
                {
                    _logger.LogWarning("Sitting not found with id: {Id}", id);
                    return NotFound();
                }

                return View(sitting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching details for sitting id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Restaurants = await _context.Restaurants.ToListAsync();
                ViewBag.SittingTypes = Enum.GetValues(typeof(SittingType)).Cast<SittingType>();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while preparing the Create view");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sitting sitting, int restaurantId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var overlappingSittings = await _context.Sittings
                        .Where(s => s.RestaurantId == restaurantId
                                    && s.End > sitting.Start
                                    && s.Start < sitting.End)
                        .ToListAsync();

                    if (overlappingSittings.Any())
                    {
                        ModelState.AddModelError("", "The selected time overlaps with an existing sitting in the restaurant.");
                    }
                    else
                    {
                        sitting.RestaurantId = restaurantId;
                        _context.Add(sitting);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while creating a new sitting");
                    return StatusCode(500, "Internal server error");
                }
            }

            ViewBag.Restaurants = await _context.Restaurants.ToListAsync();
            ViewBag.SittingTypes = Enum.GetValues(typeof(SittingType)).Cast<SittingType>();
            return View(sitting);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                _logger.LogWarning("Edit action called with null id");
                return NotFound();
            }

            try
            {
                var sitting = await _context.Sittings
                    .Include(s => s.Restaurant)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sitting == null)
                {
                    _logger.LogWarning("Sitting not found with id: {Id}", id);
                    return NotFound();
                }

                ViewBag.Restaurants = await _context.Restaurants.ToListAsync();
                return View(sitting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while preparing the Edit view for sitting id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sitting sitting)
        {
            if (id != sitting.Id)
            {
                _logger.LogWarning("Edit action called with mismatched id: {Id} and sitting.Id: {SittingId}", id, sitting.Id);
                return NotFound();
            }

            var restaurantExists = await _context.Restaurants.AnyAsync(r => r.Id == sitting.RestaurantId);
            if (!restaurantExists)
            {
                ModelState.AddModelError("RestaurantId", "The selected restaurant does not exist.");
                ViewBag.Restaurants = await _context.Restaurants.ToListAsync();
                return View(sitting);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sitting);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!await _context.Sittings.AnyAsync(e => e.Id == sitting.Id))
                    {
                        _logger.LogWarning("Concurrency error: Sitting not found with id: {Id}", sitting.Id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error while updating sitting id: {Id}", sitting.Id);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating sitting id: {Id}", sitting.Id);
                    return StatusCode(500, "Internal server error");
                }
            }

            ViewBag.Restaurants = await _context.Restaurants.ToListAsync();
            return View(sitting);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                _logger.LogWarning("Delete action called with null id");
                return NotFound();
            }

            try
            {
                var sitting = await _context.Sittings.FirstOrDefaultAsync(m => m.Id == id);

                if (sitting == null)
                {
                    _logger.LogWarning("Sitting not found with id: {Id}", id);
                    return NotFound();
                }

                return View(sitting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while preparing the Delete view for sitting id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var sitting = await _context.Sittings.FindAsync(id);
                if (sitting != null)
                {
                    _context.Sittings.Remove(sitting);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting sitting id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
