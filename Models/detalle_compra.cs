using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class detalle_compra
{
    public int iddetalle_compra { get; set; }

    public int cantidad { get; set; }

    public decimal costo_unitario { get; set; }

    public decimal subtotal { get; set; }

    public int compra_idcompra { get; set; }

    public int producto_idproducto { get; set; }

    public virtual compra compra_idcompraNavigation { get; set; } = null!;

    public virtual producto producto_idproductoNavigation { get; set; } = null!;
}
