using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace InternalControlApp.Models
{
    public class CreateElementPtciViewModel
    {
        [Display(Name = "NGCI")]
        public string? Ngci { get; set; }

        [Display(Name = "No. Elemento de Control")]
        public string? ControlNumber { get; set; }

        [Required(ErrorMessage = "El Elemento de Control es obligatorio.")]
        [Display(Name = "Elemento de Control")]
        public string ControlElement { get; set; } = "";

        public List<CreateActionPtciViewModel> Actions { get; set; } = new List<CreateActionPtciViewModel>();

        // Propiedades para llenar los menús desplegables
        public List<SelectListItem>? UnitsList { get; set; }
        public List<SelectListItem>? UsersList { get; set; }
    }
}