using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class CreateRiskPtarViewModel
    {
        [Required(ErrorMessage = "El No. de Riesgo es obligatorio.")]
        [Display(Name = "No. Riesgo")]
        public string RiskNumber { get; set; } = "";

        [Required(ErrorMessage = "La Descripción del Riesgo es obligatoria.")]
        [Display(Name = "Descripción del Riesgo")]
        public string Description { get; set; } = "";

        [Display(Name = "Clasificación del Riesgo")]
        public string? RiskClassification { get; set; }

        [Display(Name = "Grado de Impacto")]
        public int? ImpactGrade { get; set; }

        [Display(Name = "Probabilidad de Ocurrencia")]
        public int? OccurrenceProbability { get; set; }

        [Display(Name = "Cuadrante")]
        public string? Quadrant { get; set; }

        [Display(Name = "Estrategia")]
        public string? Strategy { get; set; }

        // Lista para los hijos (Factores de Riesgo)
        public List<CreateFactorPtarViewModel> Factors { get; set; } = new List<CreateFactorPtarViewModel>();

        // Listas para los menús desplegables
        public List<SelectListItem>? UnitsList { get; set; }
        public List<SelectListItem>? UsersList { get; set; }
    }
}