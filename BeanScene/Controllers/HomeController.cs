using BeanScene.Data;
using BeanScene.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeanScene.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Constructor to initialize the controller with dependencies
        public HomeController(ApplicationDbContext context,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager,ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Home/Index
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Index action called.");

            // Ensure required roles exist
            foreach (string r in new[] { "Manager", "Admin", "Staff", "Member" })
            {
                if (!await _roleManager.RoleExistsAsync(r))
                {
                    _logger.LogInformation("Creating role: {Role}", r);
                    await _roleManager.CreateAsync(new IdentityRole(r));
                }
            }

            // If the user is authenticated, ensure they are associated with a Person entity
            if (User.Identity!.IsAuthenticated)
            {
                _logger.LogInformation("User is authenticated.");

                var result = await EnsurePersonAssociation();
                if (result is UnauthorizedResult || result is NotFoundResult)
                {
                    _logger.LogWarning("EnsurePersonAssociation returned {Result}", result);
                    return result;
                }
            }

            return View();
        }

        // Method to ensure the authenticated user is associated with a Person entity
        public async Task<IActionResult> EnsurePersonAssociation()
        {
            _logger.LogInformation("EnsurePersonAssociation action called.");

            // Get the user's email
            var userEmail = User.Identity!.Name;
            if (userEmail == null)
            {
                _logger.LogWarning("User email is null.");
                return Unauthorized();
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Email}", userEmail);
                return NotFound("User not found.");
            }

            // Ensure the user has at least one role
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Count == 0)
            {
                _logger.LogInformation("User has no roles, assigning 'Member' role.");
                var roleResult = await _userManager.AddToRoleAsync(user, "Member");
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to assign 'Member' role to user: {Email}", userEmail);
                    return BadRequest("Failed to assign 'Member' role.");
                }
            }

            // Find or create a Person entity associated with the user
            var person = _context.Persons
                .AsEnumerable()
                .FirstOrDefault(p => string.Equals(p.Email, userEmail, StringComparison.OrdinalIgnoreCase));

            if (person != null)
            {
                if (string.IsNullOrEmpty(person.UserId))
                {
                    _logger.LogInformation("Associating user {UserId} with person {PersonId}.", user.Id, person.Id);
                    person.UserId = user.Id;
                    _context.Persons.Update(person);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                _logger.LogInformation("Creating new person for user {UserId}.", user.Id);
                person = new Person
                {
                    Name = user.UserName,
                    Email = userEmail,
                    UserId = user.Id
                };

                _context.Persons.Add(person);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}
