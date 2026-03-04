using InternalControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace InternalControlApp.Controllers
{
    public class CoordinadorController : Controller
    {
        private readonly InternalControlDbContext _context;

        public CoordinadorController(InternalControlDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? searchString,
            DateTime? reviewStartDate,
            DateTime? reviewEndDate,
            int? pagePendientes,
            int? pageHistorial,
            int? pageSize)
        {
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

            if (!String.IsNullOrEmpty(searchString))
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

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideReviewHistory()
        {
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
            var delivery = await _context.Deliveries.FindAsync(DeliveryId);
            if (delivery == null) return NotFound();

            delivery.DirectorFeedback = DirectorFeedback;
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
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor, seleccione un archivo para agregar.";
                return RedirectToAction("Review", new { id = deliveryId });
            }

            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
            {
                return NotFound();
            }

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