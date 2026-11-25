using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class usuario
{
    public int idusuario { get; set; }

    public string nombre { get; set; } = null!;

    public string apellido { get; set; } = null!;

    public string correo { get; set; } = null!;

    public string password { get; set; } = null!;

    public string? estado { get; set; }

    public int rol_idrol { get; set; }

    public virtual ICollection<compra> compras { get; set; } = new List<compra>();

    public virtual rol rol_idrolNavigation { get; set; } = null!;

    public virtual ICollection<venta> venta { get; set; } = new List<venta>();
}
