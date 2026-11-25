using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SistemaInventario.Data;
using SistemaInventario.Models;
using SistemaInventario.ViewModels;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador,Vendedor")]
public class VentasController : Controller
{
    private static readonly string[] MetodosPago = ["Efectivo", "QR", "Tarjeta"];
    private const decimal MaximoPrecio = 99999999.99m;
    private const decimal MaximoTotal = 9999999999.99m;
    private readonly AppDbContext _context;

    public VentasController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var ventas = await _context.ventas.AsNoTracking()
            .OrderByDescending(v => v.fecha_venta)
            .ThenByDescending(v => v.idventa)
            .Select(v => new VentaListadoViewModel
            {
                IdVenta = v.idventa,
                FechaVenta = v.fecha_venta,
                Cliente = v.cliente_idclienteNavigation == null
                    ? "Venta general"
                    : v.cliente_idclienteNavigation.nombre + " " + (v.cliente_idclienteNavigation.apellido ?? ""),
                Usuario = v.usuario_idusuarioNavigation.nombre + " " + v.usuario_idusuarioNavigation.apellido,
                Total = v.total,
                MetodoPago = v.metodo_pago,
                Estado = v.estado
            })
            .ToListAsync();

        return View(ventas);
    }

    public async Task<IActionResult> Create()
    {
        var model = new VentaCreateViewModel
        {
            MetodoPago = "Efectivo",
            UsuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };
        ViewBag.UsuarioActual = User.Identity?.Name;
        await CargarOpcionesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VentaCreateViewModel model)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioActualId))
            return Challenge();
        model.UsuarioId = usuarioActualId;
        ViewBag.UsuarioActual = User.Identity?.Name;
        model.InventarioIds ??= [];
        if (model.ClienteId is null || !await _context.clientes.AnyAsync(c => c.idcliente == model.ClienteId.Value && c.activo))
            ModelState.AddModelError(nameof(model.ClienteId), "Selecciona un cliente activo.");

        if (!await _context.usuarios.AnyAsync(u =>
                u.idusuario == usuarioActualId && u.estado == "Activo" && u.rol_idrolNavigation.activo))
        {
            return Challenge();
        }

        if (model.MetodoPago is null || !MetodosPago.Contains(model.MetodoPago, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.MetodoPago), "Selecciona Efectivo, QR o Tarjeta.");

        if (model.InventarioIds.Count == 0)
            ModelState.AddModelError(nameof(model.InventarioIds), "Selecciona al menos un equipo disponible.");
        if (model.InventarioIds.Any(id => id <= 0))
            ModelState.AddModelError(nameof(model.InventarioIds), "La selección incluye un equipo inválido.");
        if (model.InventarioIds.Count != model.InventarioIds.Distinct().Count())
            ModelState.AddModelError(nameof(model.InventarioIds), "No selecciones dos veces el mismo equipo.");

        if (!ModelState.IsValid)
        {
            await CargarOpcionesAsync(model);
            return View(model);
        }

        var ids = model.InventarioIds.Distinct().ToArray();
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            var equipos = await _context.inventarios
                .Include(i => i.producto_idproductoNavigation)
                .Include(i => i.detalle_venta)
                .Where(i => ids.Contains(i.idinventario))
                .ToListAsync();

            if (equipos.Count != ids.Length)
                ModelState.AddModelError(nameof(model.InventarioIds), "Uno o más equipos ya no existen. Actualiza la página y vuelve a seleccionar.");

            if (equipos.Any(i => i.estado != "Disponible" || i.detalle_venta is not null))
                ModelState.AddModelError(nameof(model.InventarioIds), "Uno o más equipos ya no están disponibles para una nueva venta.");

            if (equipos.Any(i => !i.producto_idproductoNavigation.activo))
                ModelState.AddModelError(nameof(model.InventarioIds), "No se puede vender una unidad de un producto inactivo.");

            if (equipos.Any(i => i.producto_idproductoNavigation.precio_venta < 0 ||
                                 i.producto_idproductoNavigation.precio_venta > MaximoPrecio ||
                                 decimal.Round(i.producto_idproductoNavigation.precio_venta, 2) != i.producto_idproductoNavigation.precio_venta))
            {
                ModelState.AddModelError(nameof(model.InventarioIds), "Un producto tiene un precio no válido para la precisión de la base de datos.");
            }

            var total = equipos.Sum(i => i.producto_idproductoNavigation.precio_venta);
            if (total > MaximoTotal)
                ModelState.AddModelError(nameof(model.InventarioIds), "El total de la venta supera el máximo permitido.");

            if (!ModelState.IsValid)
            {
                await transaccion.RollbackAsync();
                await CargarOpcionesAsync(model);
                return View(model);
            }

            var venta = new venta
            {
                fecha_venta = DateTime.Now,
                cliente_idcliente = model.ClienteId!.Value,
                usuario_idusuario = usuarioActualId,
                metodo_pago = model.MetodoPago,
                estado = "Completada",
                total = total
            };

            _context.ventas.Add(venta);
            foreach (var equipo in equipos)
            {
                var precio = equipo.producto_idproductoNavigation.precio_venta;
                equipo.estado = "Vendido";
                venta.detalle_venta.Add(new detalle_venta
                {
                    inventario_idinventario = equipo.idinventario,
                    cantidad = 1,
                    precio_unitario = precio,
                    subtotal = precio
                });
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            TempData["Mensaje"] = "La venta se registró y los equipos seleccionados quedaron como Vendido.";
            return RedirectToAction(nameof(Details), new { id = venta.idventa });
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            ModelState.AddModelError(string.Empty, "No se pudo guardar la venta porque uno de los equipos pudo ser vendido al mismo tiempo. Actualiza la página e inténtalo nuevamente.");
            await CargarOpcionesAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var model = await CargarDetalleAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Anular(int? id)
    {
        if (id is null) return NotFound();
        var model = await CargarDetalleAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            var venta = await _context.ventas
                .Include(v => v.detalle_venta)
                    .ThenInclude(d => d.inventario_idinventarioNavigation)
                .FirstOrDefaultAsync(v => v.idventa == id);

            if (venta is null) return NotFound();
            if (venta.estado != "Completada")
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "Esta venta ya está anulada o no se encuentra en estado Completada.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (venta.detalle_venta.Count == 0 || venta.detalle_venta.Any(d =>
                    d.cantidad != 1 || d.inventario_idinventarioNavigation.estado != "Vendido"))
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "No se puede anular de forma segura: hay equipos sin detalle o cuyo estado ya no es Vendido.";
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var detalle in venta.detalle_venta)
                detalle.inventario_idinventarioNavigation.estado = "Disponible";

            venta.estado = "Anulada";
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            TempData["Mensaje"] = "La venta se anuló; se conservó su historial y los equipos volvieron al estado Disponible.";
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            TempData["Error"] = "No se pudo completar la anulación. No se guardaron cambios parciales.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<VentaDetalleViewModel?> CargarDetalleAsync(int id)
    {
        var venta = await _context.ventas.AsNoTracking()
            .Where(v => v.idventa == id)
            .Select(v => new VentaDetalleViewModel
            {
                IdVenta = v.idventa,
                FechaVenta = v.fecha_venta,
                Cliente = v.cliente_idclienteNavigation == null
                    ? "Venta general"
                    : v.cliente_idclienteNavigation.nombre + " " + (v.cliente_idclienteNavigation.apellido ?? ""),
                Usuario = v.usuario_idusuarioNavigation.nombre + " " + v.usuario_idusuarioNavigation.apellido,
                Total = v.total,
                MetodoPago = v.metodo_pago,
                Estado = v.estado
            })
            .FirstOrDefaultAsync();

        if (venta is null) return null;

        venta.Detalles = await _context.detalle_ventas.AsNoTracking()
            .Where(d => d.venta_idventa == id)
            .OrderBy(d => d.iddetalle_venta)
            .Select(d => new VentaLineaViewModel
            {
                Producto = d.inventario_idinventarioNavigation.producto_idproductoNavigation.nombre,
                IdInventario = d.inventario_idinventario,
                Imei = d.inventario_idinventarioNavigation.imei,
                NumeroSerie = d.inventario_idinventarioNavigation.numero_serie,
                EstadoInventario = d.inventario_idinventarioNavigation.estado,
                PrecioUnitario = d.precio_unitario,
                Cantidad = d.cantidad,
                Subtotal = d.subtotal
            })
            .ToListAsync();

        return venta;
    }

    private async Task CargarOpcionesAsync(VentaCreateViewModel model)
    {
        var clientes = await _context.clientes.AsNoTracking()
            .Where(c => c.activo)
            .OrderBy(c => c.nombre).ThenBy(c => c.apellido)
            .Select(c => new { c.idcliente, c.nombre, c.apellido })
            .ToListAsync();
        var equipos = await _context.inventarios.AsNoTracking()
            .Where(i => i.estado == "Disponible" && i.detalle_venta == null && i.producto_idproductoNavigation.activo)
            .OrderBy(i => i.producto_idproductoNavigation.nombre)
            .ThenBy(i => i.idinventario)
            .Select(i => new EquipoDisponibleViewModel
            {
                IdInventario = i.idinventario,
                Producto = i.producto_idproductoNavigation.nombre,
                Imei = i.imei,
                NumeroSerie = i.numero_serie,
                PrecioVenta = i.producto_idproductoNavigation.precio_venta
            })
            .ToListAsync();

        ViewBag.Clientes = clientes.Select(c => new SelectListItem
        {
            Value = c.idcliente.ToString(),
            Text = string.Join(" ", new[] { c.nombre, c.apellido }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Selected = c.idcliente == model.ClienteId
        }).ToList();
        ViewBag.EquiposDisponibles = equipos;
        ViewBag.MetodosPago = new SelectList(MetodosPago, model.MetodoPago);
        ViewBag.HayClientes = clientes.Count > 0;
        ViewBag.HayEquipos = equipos.Count > 0;
    }
}
