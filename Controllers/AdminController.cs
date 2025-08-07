using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        public IActionResult Login()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginCustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _adminService.LoginAdminAsync(model.Email, model.Password);
            if (admin == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            HttpContext.Session.SetString("AdminEmail", admin.Email);
            TempData["SuccessMessage"] = "Welcome, Admin!";
            return RedirectToAction("Dashboard");
        }



    }
}
