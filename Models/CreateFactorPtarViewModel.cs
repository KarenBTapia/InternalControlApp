using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class CreateFactorPtarViewModel
    {
        [Display(Name = "No. Factor")]
        public string? FactorNumber { get; set; }

        [Display(Name = "Factor de Riesgo")]
        public string? FactorDescription { get; set; }

        [Required(ErrorMessage = "La Acción de Control es obligatoria.")]
        [Display(Name = "Acción de Control")]
        public string ControlAction { get; set; } = "";

        [Required(ErrorMessage = "La Unidad Administrativa es obligatoria.")]
        [Display(Name = "Unidad Administrativa")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "El Responsable es obligatorio.")]
        [Display(Name = "Responsable")]
        public int ResponsibleUserId { get; set; }

        [Display(Name = "Fecha de Inicio")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "Fecha de Término")]
        public DateOnly? EndDate { get; set; }

        [Display(Name = "Medios de Verificación")]
        public string? VerificationMeans { get; set; }
    }
}