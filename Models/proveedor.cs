using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class proveedor
{
    public int idproveedor { get; set; }

    public string nombre { get; set; } = null!;

    public string? nit { get; set; }

    public string? telefono { get; set; }

    public string? correo { get; set; }

    public string? direccion { get; set; }

    public bool activo { get; set; } = true;

    public virtual ICollection<compra> compras { get; set; } = new List<compra>();
}
