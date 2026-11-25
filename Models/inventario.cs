using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class inventario
{
    public int idinventario { get; set; }

    public string? numero_serie { get; set; }

    public string? imei { get; set; }

    public string? estado { get; set; }

    public DateTime? fecha_ingreso { get; set; }

    public int producto_idproducto { get; set; }

    public virtual detalle_venta? detalle_venta { get; set; }

    public virtual producto producto_idproductoNavigation { get; set; } = null!;
}
