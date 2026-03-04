using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class EditActionPtciViewModel
    {
        public int ActionId { get; set; }

        [Display(Name = "Proceso")]
        public string? Process { get; set; }

        [Display(Name = "No. Acción")]
        public string? ActionNumber { get; set; }

        [Required]
        [Display(Name = "Acción de Mejora")]
        public string ImprovementAction { get; set; } = "";

        [Required]
        [Display(Name = "Unidad Administrativa")]
        public int UnitId { get; set; }

        [Required]
        [Display(Name = "Responsable")]
        public int ResponsibleUserId { get; set; }

        // --- INICIO DE LA MODIFICACIÓN ---
        [Display(Name = "Fecha de Inicio")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "Fecha de Término")]
        public DateOnly? EndDate { get; set; }
        // --- FIN DE LA MODIFICACIÓN ---

        [Display(Name = "Medios de Verificación")]
        public string? VerificationMeans { get; set; }

        public decimal? Quarter1Grade { get; set; }
        public decimal? Quarter2Grade { get; set; }
        public decimal? Quarter3Grade { get; set; }
        public decimal? Quarter4Grade { get; set; }

        [Display(Name = "1er Trimestre Comisaria")]
        public decimal? Quarter1GradeOic { get; set; }

        [Display(Name = "2do Trimestre Comisaria")]
        public decimal? Quarter2GradeOic { get; set; }

        [Display(Name = "3er Trimestre Comisaria")]
        public decimal? Quarter3GradeOic { get; set; }

        [Display(Name = "4to Trimestre Comisaria")]
        public decimal? Quarter4GradeOic { get; set; }
    }
}