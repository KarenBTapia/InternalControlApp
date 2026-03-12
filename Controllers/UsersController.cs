using InternalControlApp.Models;
using InternalControlApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace InternalControlApp.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class UsersController : Controller
    {
        private readonly InternalControlDbContext _context;
        private readonly PasswordHasher _passwordHasher;

        public UsersController(InternalControlDbContext context, PasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        private bool IsSessionValid()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            var userIdString = HttpContext.Session.GetString("UserId");

            return !string.IsNullOrEmpty(roleName) && !string.IsNullOrEmpty(userIdString);
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // --- VALIDACIÓN DE SESIÓN ---
            if (!IsSessionValid()) return RedirectToAction("Index", "Account");

            var roleName = HttpContext.Session.GetString("RoleName");
            ViewData["CurrentFilter"] = searchString;

            IQueryable<User> usersQuery = _context.Users.Include(u => u.Role);

            if (roleName == "Coordinador")
            {
                usersQuery = usersQuery.Where(u => u.Role.RoleName != "Coordinador" && u.Role.RoleName != "Superadmin");
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.FirstName.ToLower().Contains(searchLower)
                );
            }

            var users = await usersQuery.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsSessionValid()) return RedirectToAction("Index", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create()
        {
            if (!IsSessionValid()) return RedirectToAction("Index", "Account");

            var roleName = HttpContext.Session.GetString("RoleName");
            IQueryable<Role> rolesQuery = _context.Roles;

            if (roleName == "Coordinador")
            {
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Superadmin");
            }

            var viewModel = new CreateUserViewModel
            {
                RolesList = await rolesQuery.ToListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!IsSessionValid()) return RedirectToAction("Index", "Account");

            if (ModelState.IsValid)
            {
                var newUser = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    RoleId = model.RoleId!.Value,
                    PasswordHash = _passwordHasher.Hash(model.Password)
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var roleName = HttpContext.Session.GetString("RoleName");
            IQueryable<Role> rolesQuery = _context.Roles;
            if (roleName == "Coordinador")
            {
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Superadmin");
            }
            model.RolesList = await rolesQuery.ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsSessionValid()) return RedirectToAction("Index", "Account");

            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var roleName = HttpContext.Session.GetString("RoleName");
            IQueryable<Role> rolesQuery = _context.Roles;
            if (roleName == "Coordinador")
            {
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Superadmin");
            }

            var viewModel = new EditUserViewModel
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = user.RoleId,
                RolesList = await rolesQuery.ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (!IsSessionValid()) return RedirectToAction("Index", "Account");

            if (id != model.UserId) return NotFound();

            if (string.IsNullOrEmpty(model.Password) && string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (ModelState.IsValid)
            {
                var userToUpdate = await _context.Users.FindAsync(id);
                if (userToUpdate == null) return NotFound();

                userToUpdate.FirstName = model.FirstName;
                userToUpdate.LastName = model.LastName;
                userToUpdate.Email = model.Email;
                userToUpdate.RoleId = model.RoleId;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    userToUpdate.PasswordHash = _passwordHasher.Hash(model.Password);
                }

                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            var roleName = HttpContext.Session.GetString("RoleName");
            IQueryable<Role> rolesQuery = _context.Roles;
            if (roleName == "Coordinador")
            {
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Superadmin");
            }
            model.RolesList = await rolesQuery.ToListAsync();

            return View(model);
        }
    }
}