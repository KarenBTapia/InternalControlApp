using InternalControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using Microsoft.AspNetCore.Http;

namespace InternalControlApp.Controllers
{
    public class CoordinadorController : Controller
    {
        private readonly InternalControlDbContext _context;

        public CoordinadorController(InternalControlDbContext context)
        {
            _context = context;
        }

        // --- NUEVO MÉTODO DE SEGURIDAD (REBOTE INTELIGENTE) ---
        private IActionResult ValidateCoordinadorAccess()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            // Si no hay sesión, al Login
            if (string.IsNullOrEmpty(roleName)) return RedirectToAction("Index", "Account");

            // Si es Enlace intentando entrar aquí, lo rebotamos a su vista
            if (roleName == "Enlace") return RedirectToAction("Index", "Enlace");

            // Si es Coordinador o Superadmin, le damos luz verde devolviendo null
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool clear = false)
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Si hay un rebote, lo ejecutamos

            if (clear)
            {
                HttpContext.Session.Remove("Coord_SearchString");
                HttpContext.Session.Remove("Coord_StartDate");
                HttpContext.Session.Remove("Coord_EndDate");
                HttpContext.Session.Remove("Coord_PageSize");
                HttpContext.Session.Remove("Coord_PagePendientes");
                HttpContext.Session.Remove("Coord_PageHistorial");
                return RedirectToAction(nameof(Index));
            }

            string? searchString = HttpContext.Session.GetString("Coord_SearchString");
            string? startDateStr = HttpContext.Session.GetString("Coord_StartDate");
            string? endDateStr = HttpContext.Session.GetString("Coord_EndDate");
            string? pageSizeStr = HttpContext.Session.GetString("Coord_PageSize");

            DateTime? reviewStartDate = string.IsNullOrEmpty(startDateStr) ? null : DateTime.Parse(startDateStr);
            DateTime? reviewEndDate = string.IsNullOrEmpty(endDateStr) ? null : DateTime.Parse(endDateStr);
            int? pageSize = string.IsNullOrEmpty(pageSizeStr) ? null : int.Parse(pageSizeStr);

            int? pagePendientes = HttpContext.Session.GetInt32("Coord_PagePendientes");
            int? pageHistorial = HttpContext.Session.GetInt32("Coord_PageHistorial");

            return await GetDashboardData(searchString, reviewStartDate, reviewEndDate, pagePendientes, pageHistorial, pageSize);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(
            string? searchString,
            DateTime? reviewStartDate,
            DateTime? reviewEndDate,
            int? pageSize,
            int? pagePendientes,
            int? pageHistorial,
            string formAction = "")
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Aplicado

            if (formAction == "search")
            {
                if (searchString != null) HttpContext.Session.SetString("Coord_SearchString", searchString);
                else HttpContext.Session.Remove("Coord_SearchString");

                if (reviewStartDate.HasValue) HttpContext.Session.SetString("Coord_StartDate", reviewStartDate.Value.ToString("yyyy-MM-dd"));
                else HttpContext.Session.Remove("Coord_StartDate");

                if (reviewEndDate.HasValue) HttpContext.Session.SetString("Coord_EndDate", reviewEndDate.Value.ToString("yyyy-MM-dd"));
                else HttpContext.Session.Remove("Coord_EndDate");

                HttpContext.Session.Remove("Coord_PagePendientes");
                HttpContext.Session.Remove("Coord_PageHistorial");
            }
            else if (formAction == "pageSize")
            {
                if (pageSize.HasValue) HttpContext.Session.SetString("Coord_PageSize", pageSize.Value.ToString());
                HttpContext.Session.Remove("Coord_PagePendientes");
                HttpContext.Session.Remove("Coord_PageHistorial");
            }
            else if (formAction == "paginate")
            {
                if (pagePendientes.HasValue) HttpContext.Session.SetInt32("Coord_PagePendientes", pagePendientes.Value);
                if (pageHistorial.HasValue) HttpContext.Session.SetInt32("Coord_PageHistorial", pageHistorial.Value);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> GetDashboardData(
            string? searchString,
            DateTime? reviewStartDate,
            DateTime? reviewEndDate,
            int? pagePendientes,
            int? pageHistorial,
            int? pageSize)
        {
            // ... (Este método privado queda exactamente igual que antes) ...
            ViewData["CurrentSearch"] = searchString;
            int size = (pageSize.HasValue && pageSize.Value > 0) ? pageSize.Value : 10;
            ViewBag.PageSize = size;

            var pendientesQuery = _context.Deliveries
                .Include(d => d.User)
                .Include(d => d.ActionIdPtciNavigation).ThenInclude(a => a.Element)
                .Include(d => d.FactorIdPtarNavigation).ThenInclude(f => f.Risk)
                .Where(d => d.Status == "Pendiente de Revisión");

            var historialQuery = _context.Deliveries
                .Include(d => d.User)
                .Include(d => d.ActionIdPtciNavigation).ThenInclude(a => a.Element)
                .Include(d => d.FactorIdPtarNavigation).ThenInclude(f => f.Risk)
                .Where(d => (d.Status == "Aprobado" || d.Status == "Sugerencia") && !d.IsHiddenForCoordinator);

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                pendientesQuery = pendientesQuery.Where(d => (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(searchLower));
                historialQuery = historialQuery.Where(d => (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(searchLower));
            }

            if (reviewStartDate.HasValue)
            {
                historialQuery = historialQuery.Where(d => d.ReviewDate >= reviewStartDate.Value.Date);
            }
            if (reviewEndDate.HasValue)
            {
                historialQuery = historialQuery.Where(d => d.ReviewDate < reviewEndDate.Value.Date.AddDays(1));
            }

            pendientesQuery = pendientesQuery.OrderByDescending(d => d.SubmissionDate);
            historialQuery = historialQuery.OrderByDescending(d => d.ReviewDate);

            int pageNumberPendientes = pagePendientes ?? 1;
            int pageNumberHistorial = pageHistorial ?? 1;

            var pendientesList = await pendientesQuery.ToListAsync();
            var historialList = await historialQuery.ToListAsync();

            var viewModel = new DashboardViewModel
            {
                PendientesDeRevisar = pendientesList.ToPagedList(pageNumberPendientes, size),
                HistorialDeRevisiones = historialList.ToPagedList(pageNumberHistorial, size),
                CurrentSearch = searchString,
                ReviewStartDate = reviewStartDate,
                ReviewEndDate = reviewEndDate
            };

            return View("Index", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideReviewHistory()
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Aplicado

            var deliveriesToHide = await _context.Deliveries
                .Where(d => d.Status == "Aprobado" || d.Status == "Sugerencia")
                .ToListAsync();

            foreach (var delivery in deliveriesToHide)
            {
                delivery.IsHiddenForCoordinator = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "El historial de revisiones ha sido archivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportHistoryToPdf(string? searchString, DateTime? reviewStartDate, DateTime? reviewEndDate)
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Aplicado

            var historialQuery = _context.Deliveries
                .Include(d => d.User)
                .Where(d => (d.Status == "Aprobado" || d.Status == "Sugerencia") && !d.IsHiddenForCoordinator);

            if (!String.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                historialQuery = historialQuery.Where(d => (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(searchLower));
            }

            if (reviewStartDate.HasValue)
            {
                historialQuery = historialQuery.Where(d => d.ReviewDate >= reviewStartDate.Value.Date);
            }
            if (reviewEndDate.HasValue)
            {
                historialQuery = historialQuery.Where(d => d.ReviewDate < reviewEndDate.Value.Date.AddDays(1));
            }

            var data = await historialQuery.OrderByDescending(d => d.ReviewDate).ToListAsync();

            return new ViewAsPdf("HistoryPdfTemplate", data)
            {
                FileName = $"Historial_Revisiones_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        public async Task<IActionResult> Review(int? id)
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Aplicado

            if (id == null) return NotFound();

            var delivery = await _context.Deliveries
                .Include(d => d.User)
                .Include(d => d.Attachments)
                .Include(d => d.ActionIdPtciNavigation.Element)
                .Include(d => d.FactorIdPtarNavigation.Risk)
                .FirstOrDefaultAsync(d => d.DeliveryId == id);

            if (delivery == null) return NotFound();

            var viewModel = new ReviewDeliveryViewModel
            {
                DeliveryId = delivery.DeliveryId,
                UserName = $"{delivery.User.FirstName} {delivery.User.LastName}",
                SubmissionDate = delivery.SubmissionDate,
                QuarterNumber = delivery.QuarterNumber ?? 0,
                UserComment = delivery.UserComment,
                DirectorFeedback = delivery.DirectorFeedback,
                Attachments = delivery.Attachments.ToList(),
                IsReadOnly = (delivery.Status == "Aprobado" || delivery.Status == "Sugerencia")
            };

            if (delivery.ActionIdPtci != null)
            {
                viewModel.ProgramType = "PTCI";
                viewModel.ParentTaskNumber = delivery.ActionIdPtciNavigation?.Element?.ControlNumber ?? "N/A";
                viewModel.ParentTaskDescription = delivery.ActionIdPtciNavigation?.Element?.ControlElement ?? "N/A";
                viewModel.TaskDescription = delivery.ActionIdPtciNavigation?.ImprovementAction ?? "N/A";
                viewModel.ContextualInfoLabel = "Medios de Verificación";
                viewModel.ContextualInfoText = delivery.ActionIdPtciNavigation?.VerificationMeans;
            }
            else
            {
                viewModel.ProgramType = "PTAR";
                viewModel.ParentTaskNumber = delivery.FactorIdPtarNavigation?.Risk?.RiskNumber ?? "N/A";
                viewModel.ParentTaskDescription = delivery.FactorIdPtarNavigation?.Risk?.Description ?? "N/A";
                viewModel.TaskDescription = delivery.FactorIdPtarNavigation?.ControlAction ?? "N/A";
                viewModel.ContextualInfoLabel = "Acción de Control";
                viewModel.ContextualInfoText = delivery.FactorIdPtarNavigation?.ControlAction;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int DeliveryId, string DirectorFeedback, string decision)
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Aplicado

            var delivery = await _context.Deliveries.FindAsync(DeliveryId);
            if (delivery == null) return NotFound();

            var historial = new List<ObservacionItem>();
            if (!string.IsNullOrWhiteSpace(delivery.DirectorFeedback))
            {
                try
                {
                    historial = JsonSerializer.Deserialize<List<ObservacionItem>>(delivery.DirectorFeedback) ?? new List<ObservacionItem>();
                }
                catch
                {
                    historial.Add(new ObservacionItem { Autor = "Sistema", Fecha = delivery.ReviewDate ?? DateTime.Now, Comentario = delivery.DirectorFeedback });
                }
            }

            if (!string.IsNullOrWhiteSpace(NuevasObservaciones))
            {
                string nombreUsuario = HttpContext.Session.GetString("FullName");

                if (string.IsNullOrWhiteSpace(nombreUsuario))
                {
                    nombreUsuario = "Usuario Desconocido";
                }

                historial.Add(new ObservacionItem
                {
                    Autor = nombreUsuario,
                    Fecha = DateTime.Now,
                    Comentario = NuevasObservaciones
                });
            }

            delivery.DirectorFeedback = JsonSerializer.Serialize(historial);
//             delivery.DirectorFeedback = DirectorFeedback;
            delivery.Status = decision;
            delivery.ReviewDate = DateTime.Now;

            if (decision == "Aprobado")
            {
                if (delivery.ActionIdPtci != null)
                {
                    var action = await _context.ImprovementActionsPtcis.FindAsync(delivery.ActionIdPtci);
                    if (action != null)
                    {
                        switch (delivery.QuarterNumber)
                        {
                            case 1: action.Quarter1Grade = delivery.Grade; break;
                            case 2: action.Quarter2Grade = delivery.Grade; break;
                            case 3: action.Quarter3Grade = delivery.Grade; break;
                            case 4: action.Quarter4Grade = delivery.Grade; break;
                        }
                    }
                }
                else if (delivery.FactorIdPtar != null)
                {
                    var factor = await _context.RiskFactorsPtars.FindAsync(delivery.FactorIdPtar);
                    if (factor != null)
                    {
                        switch (delivery.QuarterNumber)
                        {
                            case 1: factor.Quarter1Grade = delivery.Grade; break;
                            case 2: factor.Quarter2Grade = delivery.Grade; break;
                            case 3: factor.Quarter3Grade = delivery.Grade; break;
                            case 4: factor.Quarter4Grade = delivery.Grade; break;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttachment(int deliveryId, IFormFile file)
        {
            var access = ValidateCoordinadorAccess();
            if (access != null) return access; // <-- Aplicado

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor, seleccione un archivo para agregar.";
                return RedirectToAction("Review", new { id = deliveryId });
            }

            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null) return NotFound();

            try
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var newAttachment = new Attachment
                {
                    DeliveryId = deliveryId,
                    OriginalFileName = "[C]_" + file.FileName,
                    StoragePath = Path.Combine("uploads", uniqueFileName).Replace('\\', '/')
                };

                _context.Attachments.Add(newAttachment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Archivo agregado a la entrega correctamente.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al guardar el archivo.";
            }

            return RedirectToAction("Review", new { id = deliveryId });
        }
    }
}