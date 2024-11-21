using BeanScene.Data;
using BeanScene.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeanScene.Controllers
{
    
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(ApplicationDbContext context,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager,ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<IActionResult> Index()
        {
            foreach (string r in new[] {"Admin","Staff"}){
                if(!await _roleManager.RoleExistsAsync(r)){
                    await _roleManager.CreateAsync(new IdentityRole(r));
                }
            }

            
            //var admin=await _userManager.FindByNameAsync("admin@BeanScene");
            //var resultAdmin=await _userManager.AddToRoleAsync(admin!,"Admin");

            //var staff=await _userManager.FindByNameAsync("staff@BeanScene");
            // resultStaff=await _userManager.AddToRoleAsync(staff!,"Staff");



        if (User.Identity!.IsAuthenticated) // Only call if the user is logged in
        {
                var result = await EnsurePersonAssociation();
                if (result is UnauthorizedResult || result is NotFoundResult)
                {
                    return result; // Handle unauthorized or user-not-found scenarios
                }
        }

            return View();
        }








        public async Task<IActionResult> EnsurePersonAssociation()
        {
            // Get the logged-in user's email
            var userEmail = User.Identity!.Name; // The logged-in user's email
            if (userEmail == null)
            {
                return Unauthorized(); // Ensure the user is authenticated
            }

            // Retrieve the logged-in user from Identity
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Perform case-insensitive comparison for Person's email
            var person = _context.Persons
            .AsEnumerable() // Convert to in-memory for case-insensitive comparison
            .FirstOrDefault(p => string.Equals(p.Email, userEmail, StringComparison.OrdinalIgnoreCase));


            if (person != null)
            {
                // Associate the IdentityUser with the Person if not already associated
                if (string.IsNullOrEmpty(person.UserId))
                {
                    person.UserId = user.Id; // Set the UserId foreign key
                    _context.Persons.Update(person);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Optionally, create a new Person record if none exists
                person = new Person
                {
                    Name = user.UserName, // You may need to customize this
                    Email = userEmail,
                    UserId = user.Id // Associate with the logged-in user
                };

                _context.Persons.Add(person);
                await _context.SaveChangesAsync();
            }

            return Ok(); // Or redirect to an appropriate action
        }

        
    }
}
