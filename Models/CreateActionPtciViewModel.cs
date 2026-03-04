using System.ComponentModel.DataAnnotations;
namespace InternalControlApp.Models
{
    public class CreateActionPtciViewModel
    {
        [Display(Name = "Proceso")]
        public string? Process { get; set; }

        [Display(Name = "No. Acción")]
        public string? ActionNumber { get; set; }

        [Required(ErrorMessage = "La Acción de Mejora es obligatoria.")]
        [Display(Name = "Acción de Mejora")]
        public string ImprovementAction { get; set; } = "";

        [Required(ErrorMessage = "La Unidad Administrativa es obligatoria.")]
        [Display(Name = "Unidad Administrativa")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "El Responsable es obligatorio.")]
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
    }
}