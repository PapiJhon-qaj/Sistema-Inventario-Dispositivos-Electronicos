using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class categoria
{
    public int idcategoria { get; set; }

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool activo { get; set; } = true;

    public virtual ICollection<producto> productos { get; set; } = new List<producto>();
}
