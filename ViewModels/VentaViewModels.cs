namespace SistemaInventario.ViewModels;

public sealed class VentaCreateViewModel
{
    public int? ClienteId { get; set; }
    public int? UsuarioId { get; set; }
    public string? MetodoPago { get; set; }
    public List<int> InventarioIds { get; set; } = [];
}

public sealed class EquipoDisponibleViewModel
{
    public int IdInventario { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public string? NumeroSerie { get; set; }
    public decimal PrecioVenta { get; set; }
}

public sealed class VentaListadoViewModel
{
    public int IdVenta { get; set; }
    public DateTime? FechaVenta { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public string? Estado { get; set; }
}

public sealed class VentaDetalleViewModel
{
    public int IdVenta { get; set; }
    public DateTime? FechaVenta { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public string? Estado { get; set; }
    public List<VentaLineaViewModel> Detalles { get; set; } = [];
}

public sealed class VentaLineaViewModel
{
    public string Producto { get; set; } = string.Empty;
    public int IdInventario { get; set; }
    public string? Imei { get; set; }
    public string? NumeroSerie { get; set; }
    public string? EstadoInventario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}
