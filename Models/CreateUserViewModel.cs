using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre(s)")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [Display(Name = "Apellido(s)")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = "";

        // --- CAMBIO CLAVE: Usamos int? y [Required] ---
        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [Display(Name = "Rol")]
        public int? RoleId { get; set; } // El '?' permite que el valor sea null

        public List<Role>? RolesList { get; set; }
    }
}
