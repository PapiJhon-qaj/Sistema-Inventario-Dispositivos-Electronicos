using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class detalle_venta
{
    public int iddetalle_venta { get; set; }

    public decimal precio_unitario { get; set; }

    public int cantidad { get; set; }

    public decimal subtotal { get; set; }

    public int venta_idventa { get; set; }

    public int inventario_idinventario { get; set; }

    public virtual inventario inventario_idinventarioNavigation { get; set; } = null!;

    public virtual venta venta_idventaNavigation { get; set; } = null!;
}
