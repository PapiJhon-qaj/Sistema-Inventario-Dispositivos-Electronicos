namespace SistemaInventario.ViewModels;

public sealed class UsuarioFormViewModel
{
    public int IdUsuario { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre es obligatorio.")]
    [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El apellido es obligatorio.")]
    [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "El apellido no puede superar 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El correo es obligatorio.")]
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [System.ComponentModel.DataAnnotations.StringLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string Estado { get; set; } = "Activo";

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Selecciona un rol.")]
    public int? RolId { get; set; }
}

public sealed class UsuarioListadoViewModel
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string Rol { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public int Compras { get; set; }
}

public sealed class UsuarioDetalleViewModel
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string Rol { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public int Compras { get; set; }
}

public sealed class UsuarioEliminarViewModel
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string Rol { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public int Compras { get; set; }
    public bool PuedeEliminar => Ventas == 0 && Compras == 0;
}

public sealed class RolFormViewModel
{
    public int IdRol { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class RolListadoViewModel
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int UsuariosAsignados { get; set; }
    public bool Activo { get; set; }
}

public sealed class RolEliminarViewModel
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int UsuariosAsignados { get; set; }
    public bool PuedeEliminar => UsuariosAsignados == 0;
}
