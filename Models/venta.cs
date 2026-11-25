using System;
using System.Collections.Generic;

namespace SistemaInventario.Models;

public partial class venta
{
    public int idventa { get; set; }

    public DateTime? fecha_venta { get; set; }

    public decimal total { get; set; }

    public string? metodo_pago { get; set; }

    public string? estado { get; set; }

    public int? cliente_idcliente { get; set; }

    public int usuario_idusuario { get; set; }

    public virtual cliente? cliente_idclienteNavigation { get; set; }

    public virtual ICollection<detalle_venta> detalle_venta { get; set; } = new List<detalle_venta>();

    public virtual usuario usuario_idusuarioNavigation { get; set; } = null!;
}
