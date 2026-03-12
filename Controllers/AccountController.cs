using InternalControlApp.Models;
using InternalControlApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalControlApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly InternalControlDbContext _context;
        private readonly PasswordHasher _passwordHasher;

        public AccountController(InternalControlDbContext context, PasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("SesionActiva", new CookieOptions { Path = "/" });
            return RedirectToAction("Index", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                                         .Include(u => u.Role)
                                         .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && _passwordHasher.Verify(user.PasswordHash, model.Password))
                {
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
                    HttpContext.Session.SetString("RoleName", user.Role.RoleName);

                    Response.Cookies.Append("SesionActiva", "true", new CookieOptions { HttpOnly = false, Path = "/" });

                    if (user.Role.RoleName == "Enlace")
                    {
                        bool hasPtciTasks = await _context.ImprovementActionsPtcis.AnyAsync(a => a.ResponsibleUserId == user.UserId);
                        HttpContext.Session.SetString("HasPtciTasks", hasPtciTasks.ToString());

                        bool hasPtarTasks = await _context.RiskFactorsPtars.AnyAsync(f => f.ResponsibleUserId == user.UserId);
                        HttpContext.Session.SetString("HasPtarTasks", hasPtarTasks.ToString());
                    }

                    switch (user.Role.RoleName)
                    {
                        case "Superadmin":
                        case "Coordinador":
                            return RedirectToAction("Index", "Coordinador");
                        case "Enlace":
                            return RedirectToAction("Index", "Enlace");
                        default:
                            return RedirectToAction("Index", "Account");
                    }
                }

                ModelState.AddModelError(string.Empty, "Usuario o contraseña no válidos.");
            }
            return View(model);
        }
    }
}