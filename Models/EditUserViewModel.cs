using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre(s)")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [Display(Name = "Apellido(s)")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string Email { get; set; } = "";

        // La contraseña ahora es opcional. Solo se validará si se escribe algo.
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña (dejar en blanco para no cambiar)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nueva Contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [Display(Name = "Rol")]
        public int RoleId { get; set; }

        public List<Role>? RolesList { get; set; }
    }
}