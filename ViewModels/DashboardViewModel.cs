namespace SistemaInventario.ViewModels;

public class DashboardViewModel
{
    public int Anio { get; set; }
    public int TotalProductos { get; set; }
    public int UnidadesDisponibles { get; set; }
    public int UnidadesVendidas { get; set; }
    public int TotalClientes { get; set; }
    public int TotalProveedores { get; set; }
    public int TotalCompras { get; set; }
    public int TotalVentas { get; set; }
    public int TotalUsuariosActivos { get; set; }
    public decimal ImporteTotalVentas { get; set; }
    public List<VentaRecienteViewModel> UltimasVentas { get; set; } = new();
    public List<VentaMensualViewModel> VentasMensuales { get; set; } = new();
    public List<VentaMetodoPagoViewModel> VentasPorMetodoPago { get; set; } = new();
    public List<ProductoMasVendidoViewModel> ProductosMasVendidos { get; set; } = new();
    public List<ProductoStockBajoViewModel> ProductosStockBajo { get; set; } = new();
}

public class VentaRecienteViewModel
{
    public int IdVenta { get; set; }
    public DateTime? FechaVenta { get; set; }
    public string Cliente { get; set; } = "Venta general";
    public string Usuario { get; set; } = "No disponible";
    public decimal Importe { get; set; }
    public string? MetodoPago { get; set; }
    public string? Estado { get; set; }
}

public class VentaMensualViewModel
{
    public int NumeroMes { get; set; }
    public string Mes { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Importe { get; set; }
    public double AlturaCantidad { get; set; }
    public double AlturaImporte { get; set; }
}

public class VentaMetodoPagoViewModel
{
    public string MetodoPago { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Importe { get; set; }
    public double Porcentaje { get; set; }
}

public class ProductoMasVendidoViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal Importe { get; set; }
    public double Porcentaje { get; set; }
}

public class ProductoStockBajoViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int StockMinimo { get; set; }
    public int UnidadesDisponibles { get; set; }
}
