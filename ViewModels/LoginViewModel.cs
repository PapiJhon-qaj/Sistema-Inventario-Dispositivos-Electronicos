using System.ComponentModel.DataAnnotations;

namespace SistemaInventario.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
