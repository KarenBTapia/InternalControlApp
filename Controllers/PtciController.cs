using InternalControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using Rotativa.AspNetCore;

namespace InternalControlApp.Controllers
{
    public class PtciController : Controller
    {
        private readonly InternalControlDbContext _context;
        private readonly ILogger<PtciController> _logger;

        public PtciController(InternalControlDbContext context, ILogger<PtciController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(roleName) || string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Account");
            }

            if (roleName == "Coordinador" || roleName == "Superadmin")
            {
                var allElements = await _context.ControlElementsPtcis
                    .Include(ce => ce.ImprovementActionsPtcis)
                        .ThenInclude(ia => ia.ResponsibleUser)
                    .Include(ce => ce.ImprovementActionsPtcis)
                        .ThenInclude(ia => ia.Unit)
                    .ToListAsync();
                return View(allElements);
            }
            else
            {
                int.TryParse(userIdString, out int userId);

                var elementsForEnlace = await _context.ControlElementsPtcis
                    .Where(e => e.ImprovementActionsPtcis.Any(a => a.ResponsibleUserId == userId))
                    .Include(e => e.ImprovementActionsPtcis.Where(a => a.ResponsibleUserId == userId))
                        .ThenInclude(a => a.ResponsibleUser)
                    .Include(e => e.ImprovementActionsPtcis.Where(a => a.ResponsibleUserId == userId))
                        .ThenInclude(a => a.Unit)
                    .ToListAsync();

                return View(elementsForEnlace);
            }
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateElementPtciViewModel
            {
                UnitsList = await _context.AdministrativeUnits
                    .Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName })
                    .ToListAsync(),
                UsersList = await _context.Users
                    .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" })
                    .ToListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateElementPtciViewModel model)
        {
            ModelState.Remove("UnitsList");
            ModelState.Remove("UsersList");
            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var newElement = new ControlElementsPtci
                        {
                            Ngci = model.Ngci ?? "",
                            ControlNumber = model.ControlNumber ?? "",
                            ControlElement = model.ControlElement
                        };
                        _context.ControlElementsPtcis.Add(newElement);
                        await _context.SaveChangesAsync();

                        if (model.Actions != null && model.Actions.Count > 0)
                        {
                            foreach (var actionModel in model.Actions)
                            {
                                var newAction = new ImprovementActionsPtci
                                {
                                    ElementId = newElement.ElementId,
                                    Process = actionModel.Process ?? "",
                                    ActionNumber = actionModel.ActionNumber ?? "",
                                    ImprovementAction = actionModel.ImprovementAction,
                                    UnitId = actionModel.UnitId,
                                    ResponsibleUserId = actionModel.ResponsibleUserId,
                                    // --- INICIO DE LA MODIFICACIÓN ---
                                    StartDate = actionModel.StartDate,
                                    EndDate = actionModel.EndDate,
                                    // --- FIN DE LA MODIFICACIÓN ---
                                    VerificationMeans = actionModel.VerificationMeans ?? ""
                                };
                                _context.ImprovementActionsPtcis.Add(newAction);
                            }
                            await _context.SaveChangesAsync();
                        }
                        await transaction.CommitAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error al guardar la tarea PTCI.");
                        ModelState.AddModelError("", "Ocurrió un error al guardar la tarea.");
                    }
                }
            }
            model.UnitsList = await _context.AdministrativeUnits.Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName }).ToListAsync();
            model.UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var element = await _context.ControlElementsPtcis
                .Include(e => e.ImprovementActionsPtcis)
                .FirstOrDefaultAsync(e => e.ElementId == id);
            if (element == null) return NotFound();

            var viewModel = new EditElementPtciViewModel
            {
                ElementId = element.ElementId,
                Ngci = element.Ngci,
                ControlNumber = element.ControlNumber,
                ControlElement = element.ControlElement,
                Actions = element.ImprovementActionsPtcis.Select(a => new EditActionPtciViewModel
                {
                    ActionId = a.ActionId,
                    Process = a.Process,
                    ActionNumber = a.ActionNumber,
                    ImprovementAction = a.ImprovementAction,
                    UnitId = a.UnitId,
                    ResponsibleUserId = a.ResponsibleUserId,
                    // --- INICIO DE LA MODIFICACIÓN (1/2) ---
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    // --- FIN DE LA MODIFICACIÓN (1/2) ---
                    VerificationMeans = a.VerificationMeans,
                    Quarter1Grade = a.Quarter1Grade,
                    Quarter2Grade = a.Quarter2Grade,
                    Quarter3Grade = a.Quarter3Grade,
                    Quarter4Grade = a.Quarter4Grade,
                    Quarter1GradeOic = a.Quarter1GradeOic,
                    Quarter2GradeOic = a.Quarter2GradeOic,
                    Quarter3GradeOic = a.Quarter3GradeOic,
                    Quarter4GradeOic = a.Quarter4GradeOic
                }).ToList(),
                UnitsList = await _context.AdministrativeUnits.Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName }).ToListAsync(),
                UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditElementPtciViewModel model)
        {
            if (id != model.ElementId) return NotFound();

            ModelState.Remove("UnitsList");
            ModelState.Remove("UsersList");

            if (ModelState.IsValid)
            {
                var elementToUpdate = await _context.ControlElementsPtcis
                    .Include(e => e.ImprovementActionsPtcis)
                    .FirstOrDefaultAsync(e => e.ElementId == id);

                if (elementToUpdate == null) return NotFound();

                elementToUpdate.Ngci = model.Ngci;
                elementToUpdate.ControlNumber = model.ControlNumber;
                elementToUpdate.ControlElement = model.ControlElement;

                var existingActionIds = elementToUpdate.ImprovementActionsPtcis.Select(a => a.ActionId).ToList();
                var modelActionIds = model.Actions.Select(a => a.ActionId).ToList();
                var actionsToDelete = elementToUpdate.ImprovementActionsPtcis.Where(a => !modelActionIds.Contains(a.ActionId)).ToList();

                _context.ImprovementActionsPtcis.RemoveRange(actionsToDelete);

                foreach (var actionModel in model.Actions)
                {
                    if (actionModel.ActionId > 0)
                    {
                        var actionToUpdate = elementToUpdate.ImprovementActionsPtcis.FirstOrDefault(a => a.ActionId == actionModel.ActionId);
                        if (actionToUpdate != null)
                        {
                            actionToUpdate.Process = actionModel.Process;
                            actionToUpdate.ActionNumber = actionModel.ActionNumber;
                            actionToUpdate.ImprovementAction = actionModel.ImprovementAction;
                            actionToUpdate.UnitId = actionModel.UnitId;
                            actionToUpdate.ResponsibleUserId = actionModel.ResponsibleUserId;
                            // ---
                            actionToUpdate.StartDate = actionModel.StartDate;
                            actionToUpdate.EndDate = actionModel.EndDate;
                            // ---
                            actionToUpdate.VerificationMeans = actionModel.VerificationMeans;
                            actionToUpdate.Quarter1GradeOic = actionModel.Quarter1GradeOic;
                            actionToUpdate.Quarter2GradeOic = actionModel.Quarter2GradeOic;
                            actionToUpdate.Quarter3GradeOic = actionModel.Quarter3GradeOic;
                            actionToUpdate.Quarter4GradeOic = actionModel.Quarter4GradeOic;
                        }
                    }
                    else
                    {
                        elementToUpdate.ImprovementActionsPtcis.Add(new ImprovementActionsPtci
                        {
                            Process = actionModel.Process,
                            ActionNumber = actionModel.ActionNumber,
                            ImprovementAction = actionModel.ImprovementAction,
                            UnitId = actionModel.UnitId,
                            ResponsibleUserId = actionModel.ResponsibleUserId,
                            VerificationMeans = actionModel.VerificationMeans
                        });
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error al guardar los cambios en la edición de PTCI.");
                    ModelState.AddModelError("", "No se pudieron guardar los cambios. Asegúrate de no eliminar una acción que ya tenga evidencias entregadas.");
                    model.UnitsList = await _context.AdministrativeUnits.Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName }).ToListAsync();
                    model.UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync();
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }

            model.UnitsList = await _context.AdministrativeUnits.Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName }).ToListAsync();
            model.UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> ExportToPdf()
        {
            var data = await _context.ControlElementsPtcis
                .Include(ce => ce.ImprovementActionsPtcis).ThenInclude(ia => ia.ResponsibleUser)
                .Include(ce => ce.ImprovementActionsPtcis).ThenInclude(ia => ia.Unit)
                .ToListAsync();

            return new ViewAsPdf("PtciPdfTemplate", data)
            {
                FileName = $"Reporte_PTCI_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var data = await _context.ControlElementsPtcis
                .Include(ce => ce.ImprovementActionsPtcis).ThenInclude(ia => ia.ResponsibleUser)
                .Include(ce => ce.ImprovementActionsPtcis).ThenInclude(ia => ia.Unit)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("PTCI");
                var currentRow = 1;

                #region Headers
                string[] headers = {
                    "NGCI", "No.", "Elemento de Control", "Proceso", "No.", "Acción de Mejora", "Unidad Administrativa",
                    "Responsable", "Fecha de Inicio", "Fecha de Término", "Medios de Verificación", "1er Trim Enlace", "1er Trim Comisaria", "2do Trim Enlace", "2do Trim Comisaria",
                    "3er Trim Enlace", "3er Trim Comisaria", "4to Trim Enlace", "4to Trim Comisaria"
                };
                for (int i = 0; i < headers.Length; i++) { worksheet.Cell(currentRow, i + 1).Value = headers[i]; }
                var headerRange = worksheet.Range(currentRow, 1, currentRow, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00A987");
                headerRange.Style.Font.FontColor = XLColor.White;
                #endregion

                foreach (var element in data)
                {
                    var actions = element.ImprovementActionsPtcis.ToList();
                    var firstRowForElement = currentRow + 1;
                    if (actions.Any())
                    {
                        foreach (var action in actions)
                        {
                            currentRow++;
                            worksheet.Cell(currentRow, 1).Value = element.Ngci;
                            worksheet.Cell(currentRow, 2).Value = element.ControlNumber;
                            worksheet.Cell(currentRow, 3).Value = element.ControlElement;
                            worksheet.Cell(currentRow, 4).Value = action.Process;
                            worksheet.Cell(currentRow, 5).Value = action.ActionNumber;
                            worksheet.Cell(currentRow, 6).Value = action.ImprovementAction;
                            worksheet.Cell(currentRow, 7).Value = action.Unit.UnitName;
                            worksheet.Cell(currentRow, 8).Value = action.ResponsibleUser != null ? $"{action.ResponsibleUser.FirstName} {action.ResponsibleUser.LastName}" : "";
                            worksheet.Cell(currentRow, 9).Value = action.StartDate.HasValue ? action.StartDate.Value.ToDateTime(TimeOnly.MinValue) : "";
                            worksheet.Cell(currentRow, 10).Value = action.EndDate.HasValue ? action.EndDate.Value.ToDateTime(TimeOnly.MinValue) : "";
                            worksheet.Cell(currentRow, 11).Value = action.VerificationMeans;
                            worksheet.Cell(currentRow, 12).Value = action.Quarter1Grade;
                            worksheet.Cell(currentRow, 13).Value = action.Quarter1GradeOic;
                            worksheet.Cell(currentRow, 14).Value = action.Quarter2Grade;
                            worksheet.Cell(currentRow, 15).Value = action.Quarter2GradeOic;
                            worksheet.Cell(currentRow, 16).Value = action.Quarter3Grade;
                            worksheet.Cell(currentRow, 17).Value = action.Quarter3GradeOic;
                            worksheet.Cell(currentRow, 18).Value = action.Quarter4Grade;
                            worksheet.Cell(currentRow, 19).Value = action.Quarter4GradeOic;
                        }

                        if (actions.Count > 1)
                        {
                            worksheet.Range(firstRowForElement, 1, currentRow, 1).Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            worksheet.Range(firstRowForElement, 2, currentRow, 2).Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            worksheet.Range(firstRowForElement, 3, currentRow, 3).Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                    }
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = $"Reporte_PTCI_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, contentType, fileName);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var elementToDelete = await _context.ControlElementsPtcis
                        .Include(e => e.ImprovementActionsPtcis)
                            .ThenInclude(a => a.Deliveries)
                                .ThenInclude(d => d.Attachments)
                        .FirstOrDefaultAsync(e => e.ElementId == id);

                    if (elementToDelete == null)
                    {
                        return NotFound();
                    }

                    var attachmentsToDelete = elementToDelete.ImprovementActionsPtcis.SelectMany(a => a.Deliveries.SelectMany(d => d.Attachments)).ToList();
                    var deliveriesToDelete = elementToDelete.ImprovementActionsPtcis.SelectMany(a => a.Deliveries).ToList();
                    var actionsToDelete = elementToDelete.ImprovementActionsPtcis.ToList();

                    if (attachmentsToDelete.Any())
                    {
                        _context.Attachments.RemoveRange(attachmentsToDelete);
                    }
                    if (deliveriesToDelete.Any())
                    {
                        _context.Deliveries.RemoveRange(deliveriesToDelete);
                    }
                    if (actionsToDelete.Any())
                    {
                        _context.ImprovementActionsPtcis.RemoveRange(actionsToDelete);
                    }
                    _context.ControlElementsPtcis.Remove(elementToDelete);

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al eliminar la tarea PTCI con ID {Id}", id);
                    TempData["ErrorMessage"] = "Ocurrió un error y no se pudo eliminar la tarea.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Upload(int? id)
        {
            if (id == null) return NotFound();
            var userIdString = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdString, out int userId);

            var element = await _context.ControlElementsPtcis
                .Include(e => e.ImprovementActionsPtcis).ThenInclude(a => a.Unit)
                .Include(e => e.ImprovementActionsPtcis).ThenInclude(a => a.ResponsibleUser)
                .FirstOrDefaultAsync(e => e.ElementId == id);
            if (element == null) return NotFound();

            var deliveries = await _context.Deliveries
                .Where(d => d.UserId == userId && d.ActionIdPtci.HasValue &&
                            _context.ImprovementActionsPtcis.Any(a => a.ActionId == d.ActionIdPtci.Value && a.ElementId == id))
                .ToDictionaryAsync(d => new { ActionId = d.ActionIdPtci.Value, Quarter = d.QuarterNumber.Value }, d => new { d.Status, d.Grade });

            var viewModel = new EnlaceUploadViewModel
            {
                ElementId = element.ElementId,
                Ngci = element.Ngci,
                ControlNumber = element.ControlNumber,
                ControlElement = element.ControlElement,
                Actions = element.ImprovementActionsPtcis
                    .Where(a => a.ResponsibleUserId == userId)
                    .Select(a => {
                        var q1Info = deliveries.GetValueOrDefault(new { ActionId = a.ActionId, Quarter = 1 });
                        var q2Info = deliveries.GetValueOrDefault(new { ActionId = a.ActionId, Quarter = 2 });
                        var q3Info = deliveries.GetValueOrDefault(new { ActionId = a.ActionId, Quarter = 3 });
                        var q4Info = deliveries.GetValueOrDefault(new { ActionId = a.ActionId, Quarter = 4 });

                        return new EnlaceActionViewModel
                        {
                            ActionId = a.ActionId,
                            Process = a.Process,
                            ActionNumber = a.ActionNumber,
                            ImprovementAction = a.ImprovementAction,
                            UnitName = a.Unit.UnitName,
                            ResponsibleUserName = $"{a.ResponsibleUser.FirstName} {a.ResponsibleUser.LastName}",

                            Quarter1Grade = q1Info?.Status == "Borrador" ? q1Info.Grade : a.Quarter1Grade,
                            Quarter2Grade = q2Info?.Status == "Borrador" ? q2Info.Grade : a.Quarter2Grade,
                            Quarter3Grade = q3Info?.Status == "Borrador" ? q3Info.Grade : a.Quarter3Grade,
                            Quarter4Grade = q4Info?.Status == "Borrador" ? q4Info.Grade : a.Quarter4Grade,

                            Quarter1Status = q1Info?.Status,
                            Quarter2Status = q2Info?.Status,
                            Quarter3Status = q3Info?.Status,
                            Quarter4Status = q4Info?.Status
                        };
                    }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(int ActionId, int Quarter, decimal Percentage, string? Comment, List<IFormFile> Files, int ElementId)
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId)) return Unauthorized();

            var deliveryForCheck = await _context.Deliveries
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d => d.ActionIdPtci == ActionId && d.QuarterNumber == Quarter && d.UserId == userId && (d.Status == "Borrador" || d.Status == "Sugerencia"));

            var existingAttachmentsCount = deliveryForCheck?.Attachments.Count ?? 0;

            //validacion de archivos en backend
            if ((Files == null || Files.Count == 0) && existingAttachmentsCount == 0)
            {
                return Json(new
                {
                    success = false,
                    field = "files",
                    message = "Debe seleccionar al menos un archivo."
                });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var delivery = deliveryForCheck;

                    if (delivery == null)
                    {
                        delivery = new Delivery { UserId = userId, ActionIdPtci = ActionId, QuarterNumber = Quarter };
                        _context.Deliveries.Add(delivery);
                    }

                    delivery.Status = "Borrador";
                    delivery.UserComment = Comment;
                    delivery.SubmissionDate = DateTime.Now;
                    delivery.Grade = (int)Percentage;
                    await _context.SaveChangesAsync();

                    if (Files != null && Files.Count > 0)
                    {
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        foreach (var file in Files)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadPath, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }
                            _context.Attachments.Add(new Attachment { DeliveryId = delivery.DeliveryId, OriginalFileName = file.FileName, StoragePath = Path.Combine("uploads", uniqueFileName).Replace('\\', '/') });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al guardar el borrador.");
                    return Json(new { success = false, message = "Ocurrió un error al guardar el borrador." });
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDraftDetails(int actionId, int quarter)
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId)) return Unauthorized();

            var delivery = await _context.Deliveries
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d =>
                    d.ActionIdPtci == actionId &&
                    d.QuarterNumber == quarter &&
                    d.UserId == userId &&
                    (d.Status == "Borrador" || d.Status == "Sugerencia"));

            if (delivery == null) return Json(new { success = false });

            var result = new
            {
                success = true,
                percentage = delivery.Grade,
                comment = delivery.UserComment,
                attachments = delivery.Attachments.Select(a => new { id = a.AttachmentId, fileName = a.OriginalFileName }).ToList()
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttachment([FromBody] AttachmentDeleteModel model)
        {
            if (model == null || model.AttachmentId <= 0)
            {
                return BadRequest(new { success = false, message = "ID de archivo no válido." });
            }

            var attachment = await _context.Attachments.FindAsync(model.AttachmentId);

            if (attachment == null)
            {
                return NotFound(new { success = false, message = "El archivo no fue encontrado." });
            }

            try
            {
                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.StoragePath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }

                _context.Attachments.Remove(attachment);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el archivo adjunto con ID {Id}", model.AttachmentId);
                return StatusCode(500, new { success = false, message = "Ocurrió un error en el servidor al intentar eliminar el archivo." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEvidence(int ActionId, int Quarter)
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId)) return Unauthorized();

            var deliveryToSubmit = await _context.Deliveries.FirstOrDefaultAsync(d =>
                d.ActionIdPtci == ActionId &&
                d.QuarterNumber == Quarter &&
                d.UserId == userId &&
                (d.Status == "Borrador" || d.Status == "Sugerencia"));

            var action = await _context.ImprovementActionsPtcis.FindAsync(ActionId);
            var elementId = action?.ElementId ?? 0;

            if (deliveryToSubmit != null && action != null)
            {
                try
                {
                    deliveryToSubmit.Status = "Pendiente de Revisión";
                    deliveryToSubmit.SubmissionDate = DateTime.Now;

                    if (deliveryToSubmit.Grade.HasValue)
                    {
                        switch (Quarter)
                        {
                            case 1: action.Quarter1Grade = deliveryToSubmit.Grade.Value; break;
                            case 2: action.Quarter2Grade = deliveryToSubmit.Grade.Value; break;
                            case 3: action.Quarter3Grade = deliveryToSubmit.Grade.Value; break;
                            case 4: action.Quarter4Grade = deliveryToSubmit.Grade.Value; break;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"La evidencia del trimestre {Quarter} ha sido reenviada para revisión.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar la evidencia para la acción {ActionId}, trimestre {Quarter}", ActionId, Quarter);
                    TempData["ErrorMessage"] = "Ocurrió un error al enviar la evidencia.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No se encontró una entrega para enviar. Por favor, suba sus archivos primero.";
            }

            if (elementId > 0)
            {
                return RedirectToAction("Upload", new { id = elementId });
            }

            return RedirectToAction("Index", "Enlace");
        }

    }
    public class AttachmentDeleteModel
    {
        public int AttachmentId { get; set; }
    }
}