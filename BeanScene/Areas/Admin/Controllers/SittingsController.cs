using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BeanScene.Data;
using BeanScene.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeanScene.Areas.Admin.Controllers
{
   [Authorize(Roles ="Admin,Manager"),Area("Admin")] // give access with authorize
    public class SittingsController : Controller
    {
        private readonly ApplicationDbContext _context; // connect context
        private readonly ILogger<SittingsController> _logger; // give stack

        public SittingsController(ILogger<SittingsController> logger,ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }



        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var nextDay = selectedDate.AddDays(1);

            var sittings = await _context.Sittings
                .Include(s => s.Restaurant)
                .Include(s => s.Reservations)
                .Where(s => s.Start >= selectedDate && s.Start < nextDay)
                .OrderBy(s => s.Start)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;

            return View(sittings);
        }





        // GET: Sittings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sitting = await _context.Sittings
                .Include(s => s.Reservations)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (sitting == null)
            {
                return NotFound();
            }

            return View(sitting);
        }







          // GET: Sittings/Create
        public async Task<IActionResult> Create()
        {
            // Fetch all restaurants to display in the dropdown
            ViewBag.Restaurants = await _context.Restaurants.ToListAsync();

            
           ViewBag.SittingTypes = Enum.GetValues(typeof(SittingType)).Cast<SittingType>();
            
            return View();
        }

        // POST: Sittings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sitting sitting, int restaurantId)
        {
            if (ModelState.IsValid)
            {
                // Check for overlapping sittings in the same restaurant
                var overlappingSittings = await _context.Sittings
                    .Where(s => s.RestaurantId == restaurantId 
                                && s.End > sitting.Start 
                                && s.Start < sitting.End) // Overlap condition
                    .ToListAsync();

                if (overlappingSittings.Any())
                {
                    ModelState.AddModelError("", "The selected time overlaps with an existing sitting in the restaurant.");
                }
                else
                {
                    // Associate the sitting with the selected restaurant
                    sitting.RestaurantId = restaurantId;

                    _context.Add(sitting);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            // Re-populate the dropdowns if the model state is invalid
            ViewBag.Restaurants = await _context.Restaurants.ToListAsync();
            ViewBag.SittingTypes = Enum.GetValues(typeof(SittingType)).Cast<SittingType>();

            return View(sitting);
        }







        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sitting = await _context.Sittings
                .Include(s => s.Restaurant) // Include related Restaurant
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sitting == null)
            {
                return NotFound();
            }

            // Fetch all restaurants for the dropdown
            ViewBag.Restaurants = await _context.Restaurants.ToListAsync();

            return View(sitting);
        }

    









    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Sitting sitting)
    {
        if (id != sitting.Id)
        {
            return NotFound();
        }

        // Validate the RestaurantId
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
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Sittings.AnyAsync(e => e.Id == sitting.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index)); // after go  index
        }

        ViewBag.Restaurants = await _context.Restaurants.ToListAsync(); // for drop down

        return View(sitting);
    }
















            // GET: Sittings/Delete/5
            public async Task<IActionResult> Delete(int? id)
            {
                if (id == null)
                {
                    return NotFound();
                }

                var sitting = await _context.Sittings
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (sitting == null)
                {
                    return NotFound();
                }

                return View(sitting);
            }









        // POST: Sittings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sitting = await _context.Sittings.FindAsync(id);
            if (sitting != null)
            {
                _context.Sittings.Remove(sitting);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

       


    }
}
