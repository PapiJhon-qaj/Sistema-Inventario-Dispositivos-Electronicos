using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class compra
{
    public int idcompra { get; set; }

    public DateTime? fecha_compra { get; set; }

    public string? numero_factura { get; set; }

    public decimal total { get; set; }

    public int proveedor_idproveedor { get; set; }

    public int usuario_idusuario { get; set; }

    public virtual ICollection<detalle_compra> detalle_compras { get; set; } = new List<detalle_compra>();

    public virtual proveedor proveedor_idproveedorNavigation { get; set; } = null!;

    public virtual usuario usuario_idusuarioNavigation { get; set; } = null!;
}
