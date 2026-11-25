using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class cliente
{
    public int idcliente { get; set; }

    public string nombre { get; set; } = null!;

    public string? apellido { get; set; }

    public string? documento { get; set; }

    public string? telefono { get; set; }

    public string? correo { get; set; }

    public bool activo { get; set; } = true;

    public virtual ICollection<venta> venta { get; set; } = new List<venta>();
}
