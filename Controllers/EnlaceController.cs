using InternalControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace InternalControlApp.Controllers
{
    public class EnlaceController : Controller
    {
        private readonly InternalControlDbContext _context;

        public EnlaceController(InternalControlDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? pagePendientes, int? pageHistorial)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Index", "Account");
            }

            int pageSize = 10;
            int pageNumberPendientes = pagePendientes ?? 1;
            int pageNumberHistorial = pageHistorial ?? 1;

            // --- INICIA CORRECCIÓN: Se añaden Includes para traer los datos del padre ---
            var pendientes = await _context.Deliveries
                .Include(d => d.ActionIdPtciNavigation.Element)
                .Include(d => d.FactorIdPtarNavigation.Risk)
                .Where(d => d.UserId == userId && (d.Status == "Pendiente" || d.Status == "Sugerencia"))
                .OrderByDescending(d => d.SubmissionDate)
                .ToPagedListAsync(pageNumberPendientes, pageSize);

            var historial = await _context.Deliveries
                .Include(d => d.ActionIdPtciNavigation.Element)
                .Include(d => d.FactorIdPtarNavigation.Risk)
                .Where(d => d.UserId == userId && d.Status == "Aprobado" && !d.IsHiddenForEnlace)
                .OrderByDescending(d => d.SubmissionDate)
                .ToPagedListAsync(pageNumberHistorial, pageSize);
            // --- TERMINA CORRECCIÓN ---

            var viewModel = new EnlaceDashboardViewModel
            {
                TareasPendientes = pendientes,
                HistorialDeEntregas = historial
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideApprovedHistory()
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return Unauthorized();
            }

            var deliveriesToHide = await _context.Deliveries
                .Where(d => d.UserId == userId && d.Status == "Aprobado")
                .ToListAsync();

            foreach (var delivery in deliveriesToHide)
            {
                delivery.IsHiddenForEnlace = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tu historial de evidencias aprobadas ha sido archivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Review(int? id)
        {
            if (id == null) return NotFound();

            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return Unauthorized();
            }

            var delivery = await _context.Deliveries
                .Include(d => d.Attachments)
                .Include(d => d.ActionIdPtciNavigation.Element)
                .Include(d => d.FactorIdPtarNavigation.Risk)
                .FirstOrDefaultAsync(d => d.DeliveryId == id && d.UserId == userId);

            if (delivery == null) return NotFound();

            var viewModel = new EnlaceReviewViewModel
            {
                QuarterNumber = delivery.QuarterNumber ?? 0,
                SubmissionDate = delivery.SubmissionDate,
                Status = delivery.Status,
                UserComment = delivery.UserComment,
                DirectorFeedback = delivery.DirectorFeedback,
                Attachments = delivery.Attachments.ToList()
            };

            if (delivery.ActionIdPtci != null)
            {
                viewModel.ProgramType = "PTCI";
                viewModel.ParentTaskId = delivery.ActionIdPtciNavigation!.Element.ElementId;
                viewModel.ParentTaskNumber = delivery.ActionIdPtciNavigation?.Element?.ControlNumber ?? "N/A";
                viewModel.ParentTaskDescription = delivery.ActionIdPtciNavigation?.Element?.ControlElement ?? "N/A";
                viewModel.TaskDescription = delivery.ActionIdPtciNavigation?.ImprovementAction ?? "N/A";
            }
            else
            {
                viewModel.ProgramType = "PTAR";
                viewModel.ParentTaskId = delivery.FactorIdPtarNavigation!.Risk.RiskId;
                viewModel.ParentTaskNumber = delivery.FactorIdPtarNavigation?.Risk?.RiskNumber ?? "N/A";
                viewModel.ParentTaskDescription = delivery.FactorIdPtarNavigation?.Risk?.Description ?? "N/A";
                viewModel.TaskDescription = delivery.FactorIdPtarNavigation?.ControlAction ?? "N/A";
            }

            return View(viewModel);
        }
    }
}