using X.PagedList;

namespace InternalControlApp.Models
{
    public class DashboardViewModel
    {
        public required IPagedList<Delivery> PendientesDeRevisar { get; set; }
        public required IPagedList<Delivery> HistorialDeRevisiones { get; set; }

        public string? CurrentSearch { get; set; }

        // --- INICIO DE LA MODIFICACIÓN ---
        public DateTime? ReviewStartDate { get; set; }
        public DateTime? ReviewEndDate { get; set; }
        // --- FIN DE LA MODIFICACIÓN ---
    }
}