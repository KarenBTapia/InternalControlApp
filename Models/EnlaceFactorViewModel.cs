namespace InternalControlApp.Models
{
    // Representa cada "tarjeta" de Factor de Riesgo que se muestra en la página de carga.
    public class EnlaceFactorViewModel
    {
        public int FactorId { get; set; }
        public string? FactorNumber { get; set; }
        public string ControlAction { get; set; } = "";
        public string UnitName { get; set; } = "";
        public string ResponsibleUserName { get; set; } = "";

        // Propiedades para el porcentaje
        public decimal? Quarter1Grade { get; set; }
        public decimal? Quarter2Grade { get; set; }
        public decimal? Quarter3Grade { get; set; }
        public decimal? Quarter4Grade { get; set; }

        // Propiedades para el estado de la entrega
        public string? Quarter1Status { get; set; }
        public string? Quarter2Status { get; set; }
        public string? Quarter3Status { get; set; }
        public string? Quarter4Status { get; set; }
    }
}