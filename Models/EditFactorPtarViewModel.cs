using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class EditFactorPtarViewModel
    {
        public int FactorId { get; set; }

        [Display(Name = "No. Factor")]
        public string? FactorNumber { get; set; }

        [Display(Name = "Factor de Riesgo")]
        public string? FactorDescription { get; set; }

        [Required(ErrorMessage = "La Acción de Control es obligatoria.")]
        [Display(Name = "Acción de Control")]
        public string ControlAction { get; set; } = "";

        [Required]
        [Display(Name = "Unidad Administrativa")]
        public int UnitId { get; set; }

        [Required]
        [Display(Name = "Responsable")]
        public int ResponsibleUserId { get; set; }

        [Display(Name = "Fecha de Inicio")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "Fecha de Término")]
        public DateOnly? EndDate { get; set; }

        [Display(Name = "Medios de Verificación")]
        public string? VerificationMeans { get; set; }

        // Campos para conservar calificaciones del Enlace
        public decimal? Quarter1Grade { get; set; }
        public decimal? Quarter2Grade { get; set; }
        public decimal? Quarter3Grade { get; set; }
        public decimal? Quarter4Grade { get; set; }

        // Campos para calificaciones del Coordinador (OIC)
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