using BeanScene.Data;
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

            
            var admin=await _userManager.FindByNameAsync("admin@BeanScene");
            var resultAdmin=await _userManager.AddToRoleAsync(admin!,"Admin");

            var staff=await _userManager.FindByNameAsync("staff@BeanScene");
            var resultStaff=await _userManager.AddToRoleAsync(staff!,"Staff");

            return View();
        }
    }
}
