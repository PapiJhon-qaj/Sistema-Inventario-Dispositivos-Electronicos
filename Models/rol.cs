using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class rol
{
    public int idrol { get; set; }

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool activo { get; set; } = true;

    public virtual ICollection<usuario> usuarios { get; set; } = new List<usuario>();
}
