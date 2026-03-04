using System.Collections.Generic;

namespace InternalControlApp.Models
{
    // Representa la página completa, con el padre (Riesgo) y la lista de hijos (Factores).
    public class EnlacePtarUploadViewModel
    {
        public int RiskId { get; set; }
        public string RiskNumber { get; set; } = "";
        public string Description { get; set; } = "";

        public List<EnlaceFactorViewModel> Factors { get; set; } = new List<EnlaceFactorViewModel>();
    }
}