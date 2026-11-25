using System.ComponentModel.DataAnnotations;

namespace SistemaInventario.ViewModels;

public sealed class CompraCreateViewModel
{
    [Required(ErrorMessage = "Selecciona un proveedor.")]
    public int? ProveedorId { get; set; }

    [Required(ErrorMessage = "Selecciona el usuario que registra la compra.")]
    public int? UsuarioId { get; set; }

    [Required(ErrorMessage = "La fecha de compra es obligatoria.")]
    public DateTime? FechaCompra { get; set; } = DateTime.Now;

    [StringLength(50, ErrorMessage = "El número de factura no puede superar 50 caracteres.")]
    public string? NumeroFactura { get; set; }

    public List<DetalleCompraInputViewModel> Detalles { get; set; } =
    [
        new(),
        new()
    ];
}

public sealed class DetalleCompraInputViewModel
{
    public int? ProductoId { get; set; }

    public int? Cantidad { get; set; }

    public string? CostoUnitario { get; set; }
}

public sealed class CompraListadoViewModel
{
    public int IdCompra { get; set; }
    public DateTime? FechaCompra { get; set; }
    public string? NumeroFactura { get; set; }
    public decimal Total { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
}

public sealed class CompraDetalleViewModel
{
    public int IdCompra { get; set; }
    public DateTime? FechaCompra { get; set; }
    public string? NumeroFactura { get; set; }
    public decimal Total { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public List<CompraLineaViewModel> Detalles { get; set; } = [];
}

public sealed class CompraLineaViewModel
{
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
