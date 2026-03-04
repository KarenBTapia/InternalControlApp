namespace InternalControlApp.Models
{
    public class EnlaceActionViewModel
    {
        public int ActionId { get; set; }
        public string? Process { get; set; }
        public string? ActionNumber { get; set; }
        public string ImprovementAction { get; set; } = "";
        public string UnitName { get; set; } = "";
        public string ResponsibleUserName { get; set; } = "";

        // Propiedades para el porcentaje
        public decimal? Quarter1Grade { get; set; }
        public decimal? Quarter2Grade { get; set; }
        public decimal? Quarter3Grade { get; set; }
        public decimal? Quarter4Grade { get; set; }

        // --- INICIO DE LA CORRECCIÓN: Propiedades para el estado ---
        public string? Quarter1Status { get; set; }
        public string? Quarter2Status { get; set; }
        public string? Quarter3Status { get; set; }
        public string? Quarter4Status { get; set; }
        // --- FIN DE LA CORRECCIÓN ---
    }
}