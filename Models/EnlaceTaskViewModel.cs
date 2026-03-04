namespace InternalControlApp.Models
{
    public class EnlaceTaskViewModel
    {
        // Para saber si es "PTCI" o "PTAR"
        public string ProgramType { get; set; } = "";

        // El ID de la tarea específica (ActionId o FactorId)
        public int TaskId { get; set; }

        // El ID de la tarea padre (ElementId o RiskId)
        public int ParentId { get; set; }

        // Descripción de la tarea padre
        public string ParentDescription { get; set; } = "";

        // Descripción de la tarea asignada al Enlace
        public string TaskDescription { get; set; } = "";

        // El estado de la última entrega (lo usaremos más adelante)
        public string? Status { get; set; }
    }
}