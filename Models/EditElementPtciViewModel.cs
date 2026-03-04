using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
namespace InternalControlApp.Models
{
    public class EditElementPtciViewModel
    {
        public int ElementId { get; set; }
        [Display(Name = "NGCI")]
        public string? Ngci { get; set; }
        [Display(Name = "No. Elemento de Control")]
        public string? ControlNumber { get; set; }
        [Required]
        public string ControlElement { get; set; } = "";

        public List<EditActionPtciViewModel> Actions { get; set; } = new List<EditActionPtciViewModel>();

        public List<SelectListItem>? UnitsList { get; set; }
        public List<SelectListItem>? UsersList { get; set; }
    }
}
