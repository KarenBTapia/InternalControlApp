using InternalControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using Rotativa.AspNetCore;

namespace InternalControlApp.Controllers
{
    public class PtarController : Controller
    {
        private readonly InternalControlDbContext _context;
        private readonly ILogger<PtarController> _logger;

        public PtarController(InternalControlDbContext context, ILogger<PtarController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? year)
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            var userIdString = HttpContext.Session.GetString("UserId");

            //validacion de sesion
            if (string.IsNullOrEmpty(roleName) || string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Account");
            }

            //variables para filtrado por año
            int selectedYear = year ?? DateTime.Now.Year;
            var startYearFilter = new DateOnly(selectedYear, 1, 1);
            var endYearFilter = new DateOnly(selectedYear + 1, 1, 1);

            var years = Enumerable.Range(DateTime.Now.Year - 5, 10).ToList();
            ViewBag.YearList = new SelectList(years, selectedYear);

            if (roleName == "Coordinador" || roleName == "Superadmin")
            {
                var allRisks = await _context.RisksPtars
                    .Where(r => r.RiskFactorsPtars
                        .Any(rf => rf.StartDate.HasValue &&
                                   rf.StartDate.Value >= startYearFilter &&
                                   rf.StartDate.Value < endYearFilter))
                    .Include(r => r.RiskFactorsPtars
                        .Where(rf => rf.StartDate.HasValue &&
                                     rf.StartDate.Value >= startYearFilter &&
                                     rf.StartDate.Value < endYearFilter))
                        .ThenInclude(rf => rf.ResponsibleUser)
                    .Include(r => r.RiskFactorsPtars
                        .Where(rf => rf.StartDate.HasValue &&
                                     rf.StartDate.Value >= startYearFilter &&
                                     rf.StartDate.Value < endYearFilter))
                        .ThenInclude(rf => rf.Unit)
                    .ToListAsync();

                return View(allRisks);
            }
            else
            {
                int.TryParse(userIdString, out int userId);

                var risksForEnlace = await _context.RisksPtars
                    .Where(r => r.RiskFactorsPtars
                        .Any(f => f.ResponsibleUserId == userId &&
                                  f.StartDate.HasValue &&
                                  f.StartDate.Value >= startYearFilter &&
                                  f.StartDate.Value < endYearFilter))
                    .Include(r => r.RiskFactorsPtars
                        .Where(f => f.ResponsibleUserId == userId &&
                                    f.StartDate.HasValue &&
                                    f.StartDate.Value >= startYearFilter &&
                                    f.StartDate.Value < endYearFilter))
                        .ThenInclude(f => f.ResponsibleUser)
                    .Include(r => r.RiskFactorsPtars
                        .Where(f => f.ResponsibleUserId == userId &&
                                    f.StartDate.HasValue &&
                                    f.StartDate.Value >= startYearFilter &&
                                    f.StartDate.Value < endYearFilter))
                        .ThenInclude(f => f.Unit)
                    .ToListAsync();

                return View(risksForEnlace);
            }
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateRiskPtarViewModel
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
        public async Task<IActionResult> Create(CreateRiskPtarViewModel model)
        {
            ModelState.Remove("UnitsList");
            ModelState.Remove("UsersList");

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var newRisk = new RisksPtar
                        {
                            RiskNumber = model.RiskNumber,
                            Description = model.Description,
                            RiskClassification = model.RiskClassification,
                            ImpactGrade = model.ImpactGrade,
                            OccurrenceProbability = model.OccurrenceProbability,
                            Quadrant = model.Quadrant,
                            Strategy = model.Strategy
                        };
                        _context.RisksPtars.Add(newRisk);
                        await _context.SaveChangesAsync();

                        if (model.Factors != null && model.Factors.Any())
                        {
                            foreach (var factorModel in model.Factors)
                            {
                                var newFactor = new RiskFactorsPtar
                                {
                                    RiskId = newRisk.RiskId,
                                    FactorNumber = factorModel.FactorNumber,
                                    FactorDescription = factorModel.FactorDescription,
                                    ControlAction = factorModel.ControlAction,
                                    UnitId = factorModel.UnitId,
                                    ResponsibleUserId = factorModel.ResponsibleUserId,
                                    StartDate = factorModel.StartDate,
                                    EndDate = factorModel.EndDate,
                                    VerificationMeans = factorModel.VerificationMeans
                                };
                                _context.RiskFactorsPtars.Add(newFactor);
                            }
                            await _context.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error al guardar la tarea PTAR.");
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

            var risk = await _context.RisksPtars
                .Include(r => r.RiskFactorsPtars)
                .FirstOrDefaultAsync(r => r.RiskId == id);

            if (risk == null) return NotFound();

            var viewModel = new EditRiskPtarViewModel
            {
                RiskId = risk.RiskId,
                RiskNumber = risk.RiskNumber,
                Description = risk.Description,
                RiskClassification = risk.RiskClassification,
                ImpactGrade = risk.ImpactGrade,
                OccurrenceProbability = risk.OccurrenceProbability,
                Quadrant = risk.Quadrant,
                Strategy = risk.Strategy,
                Factors = risk.RiskFactorsPtars.Select(f => new EditFactorPtarViewModel
                {
                    FactorId = f.FactorId,
                    FactorNumber = f.FactorNumber,
                    FactorDescription = f.FactorDescription,
                    ControlAction = f.ControlAction,
                    UnitId = f.UnitId,
                    ResponsibleUserId = f.ResponsibleUserId,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate,
                    VerificationMeans = f.VerificationMeans,
                    Quarter1Grade = f.Quarter1Grade,
                    Quarter2Grade = f.Quarter2Grade,
                    Quarter3Grade = f.Quarter3Grade,
                    Quarter4Grade = f.Quarter4Grade,
                    Quarter1GradeOic = f.Quarter1GradeOic,
                    Quarter2GradeOic = f.Quarter2GradeOic,
                    Quarter3GradeOic = f.Quarter3GradeOic,
                    Quarter4GradeOic = f.Quarter4GradeOic
                }).ToList(),
                UnitsList = await _context.AdministrativeUnits.Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName }).ToListAsync(),
                UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditRiskPtarViewModel model)
        {
            if (id != model.RiskId) return NotFound();

            ModelState.Remove("UnitsList");
            ModelState.Remove("UsersList");

            if (ModelState.IsValid)
            {
                var riskToUpdate = await _context.RisksPtars
                    .Include(r => r.RiskFactorsPtars)
                    .FirstOrDefaultAsync(r => r.RiskId == id);

                if (riskToUpdate == null) return NotFound();

                riskToUpdate.RiskNumber = model.RiskNumber;
                riskToUpdate.Description = model.Description;
                riskToUpdate.RiskClassification = model.RiskClassification;
                riskToUpdate.ImpactGrade = model.ImpactGrade;
                riskToUpdate.OccurrenceProbability = model.OccurrenceProbability;
                riskToUpdate.Quadrant = model.Quadrant;
                riskToUpdate.Strategy = model.Strategy;

                _context.RiskFactorsPtars.RemoveRange(riskToUpdate.RiskFactorsPtars);

                if (model.Factors != null)
                {
                    foreach (var factorModel in model.Factors)
                    {
                        riskToUpdate.RiskFactorsPtars.Add(new RiskFactorsPtar
                        {
                            FactorNumber = factorModel.FactorNumber,
                            FactorDescription = factorModel.FactorDescription,
                            ControlAction = factorModel.ControlAction,
                            UnitId = factorModel.UnitId,
                            ResponsibleUserId = factorModel.ResponsibleUserId,
                            StartDate = factorModel.StartDate,
                            EndDate = factorModel.EndDate,
                            VerificationMeans = factorModel.VerificationMeans,
                            Quarter1Grade = factorModel.Quarter1Grade,
                            Quarter2Grade = factorModel.Quarter2Grade,
                            Quarter3Grade = factorModel.Quarter3Grade,
                            Quarter4Grade = factorModel.Quarter4Grade,
                            Quarter1GradeOic = factorModel.Quarter1GradeOic,
                            Quarter2GradeOic = factorModel.Quarter2GradeOic,
                            Quarter3GradeOic = factorModel.Quarter3GradeOic,
                            Quarter4GradeOic = factorModel.Quarter4GradeOic
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            model.UnitsList = await _context.AdministrativeUnits.Select(u => new SelectListItem { Value = u.UnitId.ToString(), Text = u.UnitName }).ToListAsync();
            model.UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var data = await _context.RisksPtars
                .Include(r => r.RiskFactorsPtars).ThenInclude(rf => rf.ResponsibleUser)
                .Include(r => r.RiskFactorsPtars).ThenInclude(rf => rf.Unit)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("PTAR");
                var currentRow = 1;

                #region Headers
                string[] headers = {
                    "No. Riesgo", "Descripción del Riesgo", "Clasificación", "Grado de Impacto", "Prob. de Ocurrencia", "Cuadrante", "Estrategia",
                    "No. Factor", "Factor de Riesgo", "Acción de Control", "Unidad Administrativa", "Responsable", "Fecha de Inicio", "Fecha de Término", "Medios de Verificación",
                    "1er Trim Enlace", "1er Trim Comisaria", "2do Trim Enlace", "2do Trim Comisaria", "3er Trim Enlace", "3er Trim Comisaria", "4to Trim Enlace", "4to Trim Comisaria"
                };
                for (int i = 0; i < headers.Length; i++) { worksheet.Cell(currentRow, i + 1).Value = headers[i]; }
                var headerRange = worksheet.Range(currentRow, 1, currentRow, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0887A0");
                headerRange.Style.Font.FontColor = XLColor.White;
                #endregion

                foreach (var risk in data)
                {
                    var factors = risk.RiskFactorsPtars.ToList();
                    var firstRowForRisk = currentRow + 1;
                    if (factors.Any())
                    {
                        foreach (var factor in factors)
                        {
                            currentRow++;
                            worksheet.Cell(currentRow, 1).Value = risk.RiskNumber;
                            worksheet.Cell(currentRow, 2).Value = risk.Description;
                            worksheet.Cell(currentRow, 3).Value = risk.RiskClassification;
                            worksheet.Cell(currentRow, 4).Value = risk.ImpactGrade;
                            worksheet.Cell(currentRow, 5).Value = risk.OccurrenceProbability;
                            worksheet.Cell(currentRow, 6).Value = risk.Quadrant;
                            worksheet.Cell(currentRow, 7).Value = risk.Strategy;
                            worksheet.Cell(currentRow, 8).Value = factor.FactorNumber;
                            worksheet.Cell(currentRow, 9).Value = factor.FactorDescription;
                            worksheet.Cell(currentRow, 10).Value = factor.ControlAction;
                            worksheet.Cell(currentRow, 11).Value = factor.Unit?.UnitName;
                            worksheet.Cell(currentRow, 12).Value = factor.ResponsibleUser != null ? $"{factor.ResponsibleUser.FirstName} {factor.ResponsibleUser.LastName}" : "";
                            worksheet.Cell(currentRow, 13).Value = factor.StartDate.HasValue ? factor.StartDate.Value.ToDateTime(TimeOnly.MinValue) : "";
                            worksheet.Cell(currentRow, 14).Value = factor.EndDate.HasValue ? factor.EndDate.Value.ToDateTime(TimeOnly.MinValue) : "";
                            worksheet.Cell(currentRow, 15).Value = factor.VerificationMeans;
                            worksheet.Cell(currentRow, 16).Value = factor.Quarter1Grade;
                            worksheet.Cell(currentRow, 17).Value = factor.Quarter1GradeOic;
                            worksheet.Cell(currentRow, 18).Value = factor.Quarter2Grade;
                            worksheet.Cell(currentRow, 19).Value = factor.Quarter2GradeOic;
                            worksheet.Cell(currentRow, 20).Value = factor.Quarter3Grade;
                            worksheet.Cell(currentRow, 21).Value = factor.Quarter3GradeOic;
                            worksheet.Cell(currentRow, 22).Value = factor.Quarter4Grade;
                            worksheet.Cell(currentRow, 23).Value = factor.Quarter4GradeOic;
                        }
                        if (factors.Count > 1)
                        {
                            for (int i = 1; i <= 7; i++)
                            {
                                var rangeToMerge = worksheet.Range(firstRowForRisk, i, currentRow, i);
                                rangeToMerge.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            }
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = $"Reporte_PTAR_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, contentType, fileName);
                }
            }
        }

        public async Task<IActionResult> ExportToPdf()
        {
            var data = await _context.RisksPtars
                .Include(r => r.RiskFactorsPtars).ThenInclude(rf => rf.ResponsibleUser)
                .Include(r => r.RiskFactorsPtars).ThenInclude(rf => rf.Unit)
                .ToListAsync();

            return new ViewAsPdf("PtarPdfTemplate", data)
            {
                FileName = $"Reporte_PTAR_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A3,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(5, 5, 5, 5)
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Cargar el Riesgo padre y toda su jerarquía.
                    var riskToDelete = await _context.RisksPtars
                        .Include(r => r.RiskFactorsPtars)
                            .ThenInclude(f => f.Deliveries)
                                .ThenInclude(d => d.Attachments)
                        .FirstOrDefaultAsync(r => r.RiskId == id);

                    if (riskToDelete == null)
                    {
                        return NotFound();
                    }

                    // 2. Recopilar todos los registros a eliminar.
                    var attachmentsToDelete = riskToDelete.RiskFactorsPtars.SelectMany(f => f.Deliveries.SelectMany(d => d.Attachments)).ToList();
                    var deliveriesToDelete = riskToDelete.RiskFactorsPtars.SelectMany(f => f.Deliveries).ToList();
                    var factorsToDelete = riskToDelete.RiskFactorsPtars.ToList();

                    // 3. Borrar los registros de la base de datos.
                    if (attachmentsToDelete.Any())
                    {
                        _context.Attachments.RemoveRange(attachmentsToDelete);
                    }
                    if (deliveriesToDelete.Any())
                    {
                        _context.Deliveries.RemoveRange(deliveriesToDelete);
                    }
                    if (factorsToDelete.Any())
                    {
                        _context.RiskFactorsPtars.RemoveRange(factorsToDelete);
                    }
                    _context.RisksPtars.Remove(riskToDelete);

                    await _context.SaveChangesAsync();

                    // 4. Confirmar la transacción.
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    // 5. Revertir en caso de error.
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al eliminar la tarea PTAR con ID {Id}", id);
                    TempData["ErrorMessage"] = "Ocurrió un error y no se pudo eliminar la tarea.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Ptar/Upload/5 (Recibe el ID del Riesgo Padre)
        public async Task<IActionResult> Upload(int? id)
        {
            if (id == null) return NotFound();
            var userIdString = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdString, out int userId);

            // Buscamos el Riesgo padre y solo incluimos los factores asignados al usuario actual.
            var risk = await _context.RisksPtars
                .Include(r => r.RiskFactorsPtars.Where(f => f.ResponsibleUserId == userId))
                    .ThenInclude(f => f.Unit)
                .Include(r => r.RiskFactorsPtars.Where(f => f.ResponsibleUserId == userId))
                    .ThenInclude(f => f.ResponsibleUser)
                .FirstOrDefaultAsync(r => r.RiskId == id);

            if (risk == null) return NotFound();

            //var factorIds = risk.RiskFactorsPtars.Select(f => f.FactorId).ToList();

            // Buscamos las entregas (borradores o finales) para estas tareas y este usuario.
            var deliveries = await _context.Deliveries
                .Where(d => d.UserId == userId && d.FactorIdPtar.HasValue &&
                            _context.RiskFactorsPtars.Any(f => f.FactorId == d.FactorIdPtar.Value && f.RiskId == id))
                .ToDictionaryAsync(d => new { FactorId = d.FactorIdPtar.Value, Quarter = d.QuarterNumber.Value }, d => new { d.Status, d.Grade });

            // Construimos el ViewModel para la vista.
            var viewModel = new EnlacePtarUploadViewModel
            {
                RiskId = risk.RiskId,
                RiskNumber = risk.RiskNumber,
                Description = risk.Description,
                Factors = risk.RiskFactorsPtars
                    .Select(f => {
                        var q1Info = deliveries.GetValueOrDefault(new { FactorId = f.FactorId, Quarter = 1 });
                        var q2Info = deliveries.GetValueOrDefault(new { FactorId = f.FactorId, Quarter = 2 });
                        var q3Info = deliveries.GetValueOrDefault(new { FactorId = f.FactorId, Quarter = 3 });
                        var q4Info = deliveries.GetValueOrDefault(new { FactorId = f.FactorId, Quarter = 4 });

                        return new EnlaceFactorViewModel
                        {
                            FactorId = f.FactorId,
                            FactorNumber = f.FactorNumber,
                            ControlAction = f.ControlAction,
                            UnitName = f.Unit.UnitName,
                            ResponsibleUserName = $"{f.ResponsibleUser.FirstName} {f.ResponsibleUser.LastName}",

                            // Usamos el % del borrador si existe, si no, el de la tabla final.
                            Quarter1Grade = q1Info?.Status == "Borrador" ? q1Info.Grade : f.Quarter1Grade,
                            Quarter2Grade = q2Info?.Status == "Borrador" ? q2Info.Grade : f.Quarter2Grade,
                            Quarter3Grade = q3Info?.Status == "Borrador" ? q3Info.Grade : f.Quarter3Grade,
                            Quarter4Grade = q4Info?.Status == "Borrador" ? q4Info.Grade : f.Quarter4Grade,

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
        public async Task<IActionResult> SaveDraft(int FactorId, int Quarter, decimal Percentage, string? Comment, List<IFormFile> Files)
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId)) return Unauthorized();

            var deliveryForCheck = await _context.Deliveries
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d => d.FactorIdPtar == FactorId && d.QuarterNumber == Quarter && d.UserId == userId && (d.Status == "Borrador" || d.Status == "Sugerencia"));

            var existingAttachmentsCount = deliveryForCheck?.Attachments.Count ?? 0;


            //validacion de archivos en backend
            if ((Files == null || Files.Count == 0) && existingAttachmentsCount == 0)
            {
                return Json(new { 
                    success = false, 
                    field = "files",
                    message = "Debe seleccionar al menos un archivo." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var delivery = deliveryForCheck;

                    if (delivery == null)
                    {
                        delivery = new Delivery { UserId = userId, FactorIdPtar = FactorId, QuarterNumber = Quarter };
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
                    _logger.LogError(ex, "Error al guardar el borrador de PTAR.");
                    return Json(new { success = false, message = "Ocurrió un error al guardar el borrador." });
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDraftDetails(int factorId, int quarter)
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId)) return Unauthorized();

            var delivery = await _context.Deliveries
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d =>
                    d.FactorIdPtar == factorId &&
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
                return StatusCode(500, new { success = false, message = "Ocurrió un error en el servidor." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEvidence(int FactorId, int Quarter)
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId)) return Unauthorized();

            var deliveryToSubmit = await _context.Deliveries.FirstOrDefaultAsync(d =>
                d.FactorIdPtar == FactorId &&
                d.QuarterNumber == Quarter &&
                d.UserId == userId &&
                (d.Status == "Borrador" || d.Status == "Sugerencia"));

            var factor = await _context.RiskFactorsPtars.FindAsync(FactorId);
            var riskId = factor?.RiskId ?? 0;

            if (deliveryToSubmit != null && factor != null)
            {
                try
                {
                    deliveryToSubmit.Status = "Pendiente de Revisión";
                    deliveryToSubmit.SubmissionDate = DateTime.Now;

                    if (deliveryToSubmit.Grade.HasValue)
                    {
                        switch (Quarter)
                        {
                            case 1: factor.Quarter1Grade = deliveryToSubmit.Grade.Value; break;
                            case 2: factor.Quarter2Grade = deliveryToSubmit.Grade.Value; break;
                            case 3: factor.Quarter3Grade = deliveryToSubmit.Grade.Value; break;
                            case 4: factor.Quarter4Grade = deliveryToSubmit.Grade.Value; break;
                        }
                    }
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"La evidencia del trimestre {Quarter} ha sido enviada para revisión.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar la evidencia para el factor {FactorId}, trimestre {Quarter}", FactorId, Quarter);
                    TempData["ErrorMessage"] = "Ocurrió un error al enviar la evidencia.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No se encontró una entrega para enviar. Por favor, suba sus archivos primero.";
            }

            if (riskId > 0)
            {
                return RedirectToAction("Upload", new { id = riskId });
            }
            return RedirectToAction("Index", "Enlace");
        }

        public class AttachmentDeleteModel
        {
            public int AttachmentId { get; set; }
        }

    }
}