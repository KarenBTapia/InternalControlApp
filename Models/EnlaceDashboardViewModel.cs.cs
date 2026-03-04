using System.Collections.Generic;
using X.PagedList; // <--- AÑADIR ESTE USING

namespace InternalControlApp.Models
{
    public class EnlaceDashboardViewModel
    {
        // --- CAMBIO: Ahora son IPagedList en lugar de List ---
        public IPagedList<Delivery> TareasPendientes { get; set; }
        public IPagedList<Delivery> HistorialDeEntregas { get; set; }
    }
}