using System.Collections.Generic;

namespace InternalControlApp.Models
{
    // Representa la página completa, con el padre y la lista de hijos.
    public class EnlaceUploadViewModel
    {
        public int ElementId { get; set; }
        public string? Ngci { get; set; }
        public string? ControlNumber { get; set; }
        public string ControlElement { get; set; } = "";

        public List<EnlaceActionViewModel> Actions { get; set; } = new List<EnlaceActionViewModel>();
    }
}