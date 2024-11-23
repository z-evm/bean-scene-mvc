using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

       [HttpGet]
        public async Task<IActionResult> Index(string email)
        {
            var users = _userManager.Users.ToList();

            // Filter users by email if a search query is provided
            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var userList = users.Select(user => new
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentRoles = string.Join(", ", _userManager.GetRolesAsync(user).Result) // Fetch roles
            }).ToList();

            // Pass email query parameter to the view using ViewData
            ViewData["EmailSearch"] = email;

            return View(userList);
        }




    [HttpGet]
public async Task<IActionResult> Staff(string email)
{
    var users = _userManager.Users.ToList();

    // Filter users by email if a search query is provided
    if (!string.IsNullOrEmpty(email))
    {
        users = users.Where(u => u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var userList = new List<dynamic>();

    foreach (var user in users)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Staff")) // Fetch only users with the "Staff" role
        {
            userList.Add(new
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentRoles = string.Join(", ", roles) // List roles as a comma-separated string
            });
        }
    }

    // Pass email query parameter to the view using ViewData
    ViewData["EmailSearch"] = email;

    return View(userList); // View for Staff
}






    [HttpGet]
public async Task<IActionResult> Members(string email)
{
    var users = _userManager.Users.ToList();

    // Filter users by email if a search query is provided
    if (!string.IsNullOrEmpty(email))
    {
        users = users.Where(u => u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var userList = new List<dynamic>();

    foreach (var user in users)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Member")) // Fetch users explicitly assigned the "Member" role
        {
            userList.Add(new
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentRoles = string.Join(", ", roles) // List roles as a comma-separated string
            });
        }
    }

    // Pass email query parameter to the view using ViewData
    ViewData["EmailSearch"] = email;

    return View(userList); // View for Members
}






[HttpGet]
[Authorize(Roles = "Manager")] // Restrict access to only users with the "Manager" role
public async Task<IActionResult> Admins(string email)
{
    var users = _userManager.Users.ToList();

    // Filter users by email if a search query is provided
    if (!string.IsNullOrEmpty(email))
    {
        users = users.Where(u => u.Email!.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var adminAndStaffList = new List<dynamic>();

    foreach (var user in users)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // Check if the user has either "Admin" or "Staff" roles
        if (roles.Contains("Admin") || roles.Contains("Staff"))
        {
            adminAndStaffList.Add(new
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentRoles = string.Join(", ", roles) // List roles as a comma-separated string
            });
        }
    }

    // Pass email query parameter to the view using ViewData
    ViewData["EmailSearch"] = email;

    return View(adminAndStaffList); // View for Admins and Staff
}





        // Display the form to edit user role
        [HttpGet]
public async Task<IActionResult> Edit(string id)
{
    // Find the user by ID
    var user = await _userManager.FindByIdAsync(id);
    if (user == null)
    {
        return NotFound();
    }

    // Restrict the roles to Staff and Member
    var roles = new List<string> { "Staff", "Member" };

    // Get the current roles of the user
    var currentRoles = await _userManager.GetRolesAsync(user);

    // Prepare the model
    var model = new
    {
        UserId = user.Id,
        Email = user.Email,
        Roles = roles, // Only Staff and Member roles are available for editing
        CurrentRoles = currentRoles // Currently assigned roles
    };

    return View(model);
}




[HttpPost]
[Authorize(Roles = "Manager,Admin")] // Ensure both Managers and Admins can access this action
public async Task<IActionResult> Edit(string userId, string selectedRole)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return NotFound();
    }

    // Remove all current roles
    var currentRoles = await _userManager.GetRolesAsync(user);
    await _userManager.RemoveFromRolesAsync(user, currentRoles);

    // Define allowed roles based on the logged-in user's roles
    var allowedRoles = new List<string> { "Admin", "Staff", "Member" };

    // Validate the selected role
    if (!string.IsNullOrEmpty(selectedRole) && allowedRoles.Contains(selectedRole))
    {
        var result = await _userManager.AddToRoleAsync(user, selectedRole);
        if (!result.Succeeded)
        {
            return BadRequest("Failed to update the role.");
        }
    }
    else if (!string.IsNullOrEmpty(selectedRole))
    {
        return Forbid("You are not authorized to assign this role.");
    }

    return RedirectToAction(nameof(Staff));
}








[HttpGet]
[Authorize(Roles = "Manager")] // Ensure only Managers can access this action
public async Task<IActionResult> ManagerEdit(string id)
{
    // Find the user by ID
    var user = await _userManager.FindByIdAsync(id);
    if (user == null)
    {
        return NotFound();
    }

    // Restrict the editable roles to Admin, Staff, and Member
    var roles = new List<string> { "Admin", "Staff", "Member" };

    // Get the current roles of the user
    var currentRoles = await _userManager.GetRolesAsync(user);

    // Prepare the model
    var model = new
    {
        UserId = user.Id,
        Email = user.Email,
        Roles = roles, // Editable roles
        CurrentRoles = currentRoles // Currently assigned roles
    };

    return View(model);
}




    }
}
