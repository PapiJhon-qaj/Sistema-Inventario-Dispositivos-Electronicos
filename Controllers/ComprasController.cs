using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SistemaInventario.Data;
using SistemaInventario.Models;
using SistemaInventario.ViewModels;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class ComprasController : Controller
{
    private const decimal MaximoCostoUnitario = 99999999.99m;
    private const decimal MaximoImporte = 9999999999.99m;
    private readonly AppDbContext _context;

    public ComprasController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? busqueda)
    {
        var compras = _context.compras.AsNoTracking().AsQueryable();

        busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
        if (busqueda is not null)
        {
            compras = compras.Where(c =>
                (c.numero_factura != null && c.numero_factura.Contains(busqueda)) ||
                c.proveedor_idproveedorNavigation.nombre.Contains(busqueda));
        }

        ViewBag.Busqueda = busqueda;
        var resultado = await compras
            .OrderByDescending(c => c.fecha_compra)
            .ThenByDescending(c => c.idcompra)
            .Select(c => new CompraListadoViewModel
            {
                IdCompra = c.idcompra,
                FechaCompra = c.fecha_compra,
                NumeroFactura = c.numero_factura,
                Total = c.total,
                Proveedor = c.proveedor_idproveedorNavigation.nombre,
                Usuario = c.usuario_idusuarioNavigation.nombre + " " + c.usuario_idusuarioNavigation.apellido
            })
            .ToListAsync();

        return View(resultado);
    }

    public async Task<IActionResult> Create()
    {
        await CargarOpcionesAsync();
        return View(new CompraCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompraCreateViewModel model)
    {
        model.Detalles ??= [];
        model.NumeroFactura = TextoOpcional(model.NumeroFactura);

        if (model.ProveedorId is int proveedorId &&
            !await _context.proveedores.AnyAsync(p => p.idproveedor == proveedorId && p.activo))
        {
            ModelState.AddModelError(nameof(model.ProveedorId), "Selecciona un proveedor activo.");
        }

        if (model.UsuarioId is int usuarioId &&
            !await _context.usuarios.AnyAsync(u => u.idusuario == usuarioId && u.estado == "Activo" && u.rol_idrolNavigation.activo))
        {
            ModelState.AddModelError(nameof(model.UsuarioId), "Selecciona un usuario activo.");
        }

        if (model.NumeroFactura?.Length > 50)
            ModelState.AddModelError(nameof(model.NumeroFactura), "El número de factura no puede superar 50 caracteres.");

        var detallesValidos = ValidarYPrepararDetalles(model);
        if (!ModelState.IsValid)
        {
            await CargarOpcionesAsync(model);
            return View(model);
        }

        var productosIds = detallesValidos.Select(d => d.ProductoId).Distinct().ToArray();
        var productosExistentes = await _context.productos
            .AsNoTracking()
            .Where(p => productosIds.Contains(p.idproducto) && p.activo)
            .Select(p => p.idproducto)
            .ToListAsync();
        var productosIdSet = productosExistentes.ToHashSet();

        for (var i = 0; i < detallesValidos.Count; i++)
        {
            if (!productosIdSet.Contains(detallesValidos[i].ProductoId))
                ModelState.AddModelError($"Detalles[{detallesValidos[i].Indice}].ProductoId", "Selecciona un producto activo.");
        }

        if (!ModelState.IsValid)
        {
            await CargarOpcionesAsync(model);
            return View(model);
        }

        var total = detallesValidos.Sum(d => d.Subtotal);
        if (total > MaximoImporte)
        {
            ModelState.AddModelError(string.Empty, "El total supera el máximo permitido por la base de datos.");
            await CargarOpcionesAsync(model);
            return View(model);
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            var compra = new compra
            {
                proveedor_idproveedor = model.ProveedorId!.Value,
                usuario_idusuario = model.UsuarioId!.Value,
                fecha_compra = model.FechaCompra,
                numero_factura = model.NumeroFactura,
                total = total
            };

            _context.compras.Add(compra);
            await _context.SaveChangesAsync();

            foreach (var detalle in detallesValidos)
            {
                _context.detalle_compras.Add(new detalle_compra
                {
                    compra_idcompra = compra.idcompra,
                    producto_idproducto = detalle.ProductoId,
                    cantidad = detalle.Cantidad,
                    costo_unitario = detalle.CostoUnitario,
                    subtotal = detalle.Subtotal
                });
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            TempData["Mensaje"] = "La compra se registró con sus productos e importes calculados en el servidor.";
            return RedirectToAction(nameof(Details), new { id = compra.idcompra });
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            ModelState.AddModelError(string.Empty, "No se pudo guardar la compra. Verifica que proveedor, usuario y productos sigan registrados e inténtalo otra vez.");
            await CargarOpcionesAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var compra = await _context.compras.AsNoTracking()
            .Where(c => c.idcompra == id.Value)
            .Select(c => new CompraDetalleViewModel
            {
                IdCompra = c.idcompra,
                FechaCompra = c.fecha_compra,
                NumeroFactura = c.numero_factura,
                Total = c.total,
                Proveedor = c.proveedor_idproveedorNavigation.nombre,
                Usuario = c.usuario_idusuarioNavigation.nombre + " " + c.usuario_idusuarioNavigation.apellido
            })
            .FirstOrDefaultAsync();

        if (compra is null) return NotFound();

        compra.Detalles = await _context.detalle_compras.AsNoTracking()
            .Where(d => d.compra_idcompra == id.Value)
            .OrderBy(d => d.iddetalle_compra)
            .Select(d => new CompraLineaViewModel
            {
                Producto = d.producto_idproductoNavigation.nombre,
                Cantidad = d.cantidad,
                CostoUnitario = d.costo_unitario,
                Subtotal = d.subtotal
            })
            .ToListAsync();

        return View(compra);
    }

    private List<DetalleCompraPreparado> ValidarYPrepararDetalles(CompraCreateViewModel model)
    {
        var resultado = new List<DetalleCompraPreparado>();
        var filas = model.Detalles ?? [];
        var algunaFilaConDatos = false;

        for (var i = 0; i < filas.Count; i++)
        {
            var fila = filas[i];
            var costoTexto = fila.CostoUnitario?.Trim();
            var tieneDatos = fila.ProductoId.HasValue || fila.Cantidad.HasValue || !string.IsNullOrWhiteSpace(costoTexto);
            if (!tieneDatos) continue;

            algunaFilaConDatos = true;

            if (!fila.ProductoId.HasValue)
                ModelState.AddModelError($"Detalles[{i}].ProductoId", "Selecciona un producto para esta fila.");
            if (!fila.Cantidad.HasValue || fila.Cantidad <= 0)
                ModelState.AddModelError($"Detalles[{i}].Cantidad", "La cantidad debe ser mayor que cero.");
            decimal? costoUnitario = null;
            if (string.IsNullOrWhiteSpace(costoTexto))
            {
                ModelState.AddModelError($"Detalles[{i}].CostoUnitario", "El costo unitario es obligatorio.");
            }
            else if (!decimal.TryParse(costoTexto,
                         NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                         CultureInfo.InvariantCulture,
                         out var costoParseado))
            {
                ModelState.AddModelError($"Detalles[{i}].CostoUnitario", "Escribe un costo unitario válido con punto decimal si corresponde.");
            }
            else if (costoParseado < 0)
            {
                ModelState.AddModelError($"Detalles[{i}].CostoUnitario", "El costo unitario no puede ser negativo.");
            }
            else if (costoParseado > MaximoCostoUnitario)
            {
                ModelState.AddModelError($"Detalles[{i}].CostoUnitario", "El costo unitario supera el máximo permitido.");
            }
            else if (decimal.Round(costoParseado, 2) != costoParseado)
            {
                ModelState.AddModelError($"Detalles[{i}].CostoUnitario", "El costo unitario admite como máximo dos decimales.");
            }
            else
            {
                costoUnitario = costoParseado;
            }

            if (fila.ProductoId.HasValue && fila.Cantidad is > 0 && costoUnitario.HasValue)
            {
                var subtotal = fila.Cantidad.Value * costoUnitario.Value;
                if (subtotal > MaximoImporte)
                {
                    ModelState.AddModelError($"Detalles[{i}].Cantidad", "El subtotal de esta línea supera el máximo permitido.");
                    continue;
                }

                resultado.Add(new DetalleCompraPreparado(
                    i,
                    fila.ProductoId.Value,
                    fila.Cantidad.Value,
                    costoUnitario.Value,
                    subtotal));
            }
        }

        if (!algunaFilaConDatos)
            ModelState.AddModelError(nameof(model.Detalles), "Agrega al menos un producto a la compra.");

        return resultado;
    }

    private async Task CargarOpcionesAsync(CompraCreateViewModel? model = null)
    {
        var proveedores = await _context.proveedores.AsNoTracking()
            .Where(p => p.activo)
            .OrderBy(p => p.nombre).ThenBy(p => p.idproveedor)
            .Select(p => new { p.idproveedor, p.nombre }).ToListAsync();
        var usuarios = await _context.usuarios.AsNoTracking()
            .Where(u => u.estado == "Activo" && u.rol_idrolNavigation.activo)
            .OrderBy(u => u.nombre).ThenBy(u => u.apellido)
            .Select(u => new { u.idusuario, u.nombre, u.apellido }).ToListAsync();
        var productos = await _context.productos.AsNoTracking()
            .Where(p => p.activo)
            .OrderBy(p => p.nombre).ThenBy(p => p.idproducto)
            .Select(p => new { p.idproducto, p.nombre }).ToListAsync();

        ViewBag.Proveedores = new SelectList(proveedores, "idproveedor", "nombre", model?.ProveedorId);
        ViewBag.Usuarios = usuarios.Select(u => new SelectListItem
        {
            Value = u.idusuario.ToString(),
            Text = $"{u.nombre} {u.apellido}",
            Selected = u.idusuario == model?.UsuarioId
        }).ToList();
        ViewBag.Productos = new SelectList(productos, "idproducto", "nombre");
        ViewBag.ProductosLista = productos.Select(p => new SelectListItem
        {
            Value = p.idproducto.ToString(),
            Text = p.nombre
        }).ToList();
        ViewBag.HayProveedores = proveedores.Count > 0;
        ViewBag.HayUsuarios = usuarios.Count > 0;
        ViewBag.HayProductos = productos.Count > 0;
    }

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private sealed record DetalleCompraPreparado(
        int Indice,
        int ProductoId,
        int Cantidad,
        decimal CostoUnitario,
        decimal Subtotal);
}
