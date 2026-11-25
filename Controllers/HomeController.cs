using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;
using SistemaInventario.ViewModels;

namespace SistemaInventario.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var inicioAnio = new DateTime(hoy.Year, 1, 1);
        var inicioProximoAnio = inicioAnio.AddYears(1);
        var completadas = _context.ventas.AsNoTracking()
            .Where(v => v.estado == "Completada");

        var inventarioPorEstado = await _context.inventarios.AsNoTracking()
            .Where(i => i.estado == "Disponible" || i.estado == "Vendido")
            .GroupBy(i => i.estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var ventasPorMes = await _context.ventas.AsNoTracking()
            .Where(v => v.estado == "Completada" &&
                        v.fecha_venta.HasValue &&
                        v.fecha_venta.Value >= inicioAnio &&
                        v.fecha_venta.Value < inicioProximoAnio)
            .GroupBy(v => v.fecha_venta!.Value.Month)
            .Select(g => new
            {
                Mes = g.Key,
                Cantidad = g.Count(),
                Importe = g.Sum(v => v.total)
            })
            .ToListAsync();

        var ventasPorMetodoPagoRaw = await completadas
            .GroupBy(v => v.metodo_pago ?? "Sin especificar")
            .Select(g => new { MetodoPago = g.Key, Cantidad = g.Count(), Importe = g.Sum(v => v.total) })
            .OrderByDescending(g => g.Cantidad)
            .ToListAsync();
        var maximoMetodoPago = ventasPorMetodoPagoRaw.Count == 0 ? 0 : ventasPorMetodoPagoRaw.Max(v => v.Cantidad);
        var ventasPorMetodoPago = ventasPorMetodoPagoRaw.Select(v => new VentaMetodoPagoViewModel
        {
            MetodoPago = v.MetodoPago,
            Cantidad = v.Cantidad,
            Importe = v.Importe,
            Porcentaje = maximoMetodoPago == 0 ? 0 : v.Cantidad * 100d / maximoMetodoPago
        }).ToList();

        var productosMasVendidosRaw = await (
            from detalle in _context.detalle_ventas.AsNoTracking()
            join equipo in _context.inventarios.AsNoTracking()
                on detalle.inventario_idinventario equals equipo.idinventario
            join producto in _context.productos.AsNoTracking()
                on equipo.producto_idproducto equals producto.idproducto
            join venta in _context.ventas.AsNoTracking()
                on detalle.venta_idventa equals venta.idventa
            where venta.estado == "Completada"
            group detalle by new { producto.idproducto, producto.nombre } into grupo
            orderby grupo.Sum(d => d.cantidad) descending, grupo.Key.nombre
            select new
            {
                grupo.Key.idproducto,
                grupo.Key.nombre,
                Unidades = grupo.Sum(d => d.cantidad),
                Importe = grupo.Sum(d => d.subtotal)
            })
            .Take(5)
            .ToListAsync();
        var maximoProducto = productosMasVendidosRaw.Count == 0 ? 0 : productosMasVendidosRaw.Max(p => p.Unidades);
        var productosMasVendidos = productosMasVendidosRaw.Select(p => new ProductoMasVendidoViewModel
        {
            IdProducto = p.idproducto,
            Nombre = p.nombre,
            Unidades = p.Unidades,
            Importe = p.Importe,
            Porcentaje = maximoProducto == 0 ? 0 : p.Unidades * 100d / maximoProducto
        }).ToList();

        var maximoCantidad = ventasPorMes.Count == 0 ? 0 : ventasPorMes.Max(v => v.Cantidad);
        var maximoImporte = ventasPorMes.Count == 0 ? 0m : ventasPorMes.Max(v => v.Importe);
        var meses = CultureInfo.GetCultureInfo("es-BO").DateTimeFormat;
        var resumenMensual = Enumerable.Range(1, 12)
            .Select(numeroMes =>
            {
                var datos = ventasPorMes.FirstOrDefault(v => v.Mes == numeroMes);
                var cantidad = datos?.Cantidad ?? 0;
                var importe = datos?.Importe ?? 0m;

                return new VentaMensualViewModel
                {
                    NumeroMes = numeroMes,
                    Mes = meses.GetAbbreviatedMonthName(numeroMes),
                    Cantidad = cantidad,
                    Importe = importe,
                    AlturaCantidad = maximoCantidad == 0 ? 0 : cantidad * 100d / maximoCantidad,
                    AlturaImporte = maximoImporte == 0 ? 0 : (double)(importe * 100m / maximoImporte)
                };
            })
            .ToList();

        var productosStockBajo = await _context.productos.AsNoTracking()
            .Where(p => p.activo)
            .Select(p => new
            {
                Producto = p,
                Disponibles = p.inventarios.Count(i => i.estado == "Disponible")
            })
            .Where(p => p.Producto.stock_minimo.HasValue &&
                        p.Disponibles <= p.Producto.stock_minimo.Value)
            .OrderBy(p => p.Disponibles)
            .ThenBy(p => p.Producto.nombre)
            .Select(p => new ProductoStockBajoViewModel
            {
                IdProducto = p.Producto.idproducto,
                Nombre = p.Producto.nombre,
                Marca = p.Producto.marca,
                Modelo = p.Producto.modelo,
                StockMinimo = p.Producto.stock_minimo!.Value,
                UnidadesDisponibles = p.Disponibles
            })
            .ToListAsync();

        var model = new DashboardViewModel
        {
            Anio = hoy.Year,
            TotalProductos = await _context.productos.AsNoTracking().CountAsync(),
            UnidadesDisponibles = inventarioPorEstado.FirstOrDefault(i => i.Estado == "Disponible")?.Cantidad ?? 0,
            UnidadesVendidas = inventarioPorEstado.FirstOrDefault(i => i.Estado == "Vendido")?.Cantidad ?? 0,
            TotalClientes = await _context.clientes.AsNoTracking().CountAsync(),
            TotalProveedores = await _context.proveedores.AsNoTracking().CountAsync(),
            TotalCompras = await _context.compras.AsNoTracking().CountAsync(),
            TotalVentas = await completadas.CountAsync(),
            ImporteTotalVentas = await completadas.Select(v => (decimal?)v.total).SumAsync() ?? 0m,
            TotalUsuariosActivos = await _context.usuarios.AsNoTracking()
                .CountAsync(u => u.estado == "Activo" && u.rol_idrolNavigation.activo),
            VentasMensuales = resumenMensual,
            VentasPorMetodoPago = ventasPorMetodoPago,
            ProductosMasVendidos = productosMasVendidos,
            ProductosStockBajo = productosStockBajo,
            UltimasVentas = await _context.ventas.AsNoTracking()
                .OrderByDescending(v => v.fecha_venta)
                .ThenByDescending(v => v.idventa)
                .Take(5)
                .Select(v => new VentaRecienteViewModel
                {
                    IdVenta = v.idventa,
                    FechaVenta = v.fecha_venta,
                    Cliente = v.cliente_idclienteNavigation == null
                        ? "Venta general"
                        : v.cliente_idclienteNavigation.nombre + " " + (v.cliente_idclienteNavigation.apellido ?? ""),
                    Usuario = v.usuario_idusuarioNavigation == null
                        ? "No disponible"
                        : v.usuario_idusuarioNavigation.nombre + " " + v.usuario_idusuarioNavigation.apellido,
                    Importe = v.total,
                    MetodoPago = v.metodo_pago,
                    Estado = v.estado
                })
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
