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

            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var userList = new List<dynamic>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    CurrentRoles = string.Join(", ", roles)
                });
            }

            ViewData["EmailSearch"] = email;
            return View(userList);
        }

        [HttpGet]
        public async Task<IActionResult> Staff(string email)
        {
            var users = _userManager.Users.ToList();

            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var userList = new List<dynamic>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Staff"))
                {
                    userList.Add(new
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        CurrentRoles = string.Join(", ", roles)
                    });
                }
            }

            ViewData["EmailSearch"] = email;
            return View(userList);
        }

        [HttpGet]
        public async Task<IActionResult> Members(string email)
        {
            var users = _userManager.Users.ToList();

            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var userList = new List<dynamic>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Member"))
                {
                    userList.Add(new
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        CurrentRoles = string.Join(", ", roles)
                    });
                }
            }

            ViewData["EmailSearch"] = email;
            return View(userList);
        }

        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Admins(string email)
        {
            var users = _userManager.Users.ToList();

            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var adminAndStaffList = new List<dynamic>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin") || roles.Contains("Staff"))
                {
                    adminAndStaffList.Add(new
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        CurrentRoles = string.Join(", ", roles)
                    });
                }
            }

            ViewData["EmailSearch"] = email;
            return View(adminAndStaffList);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = new List<string> { "Staff", "Member" };
            var currentRoles = await _userManager.GetRolesAsync(user);

            var model = new
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles,
                CurrentRoles = currentRoles
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Edit(string userId, string selectedRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var allowedRoles = new List<string> { "Admin", "Staff", "Member" };

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
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerEdit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = new List<string> { "Admin", "Staff", "Member" };
            var currentRoles = await _userManager.GetRolesAsync(user);

            var model = new
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles,
                CurrentRoles = currentRoles
            };

            return View(model);
        }
    }
}
