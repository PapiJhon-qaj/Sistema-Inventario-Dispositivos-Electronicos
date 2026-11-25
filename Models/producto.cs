using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class producto
{
    public int idproducto { get; set; }

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public string? marca { get; set; }

    public string? modelo { get; set; }

    public string? codigo_barras { get; set; }

    public decimal precio_venta { get; set; }

    public int? stock_minimo { get; set; }

    public int categoria_idcategoria { get; set; }

    public bool activo { get; set; } = true;

    public virtual categoria categoria_idcategoriaNavigation { get; set; } = null!;

    public virtual ICollection<detalle_compra> detalle_compras { get; set; } = new List<detalle_compra>();

    public virtual ICollection<inventario> inventarios { get; set; } = new List<inventario>();
}
