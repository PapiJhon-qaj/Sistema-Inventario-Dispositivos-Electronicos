using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class InventariosController : Controller
{
    private static readonly string[] EstadosValidos = ["Disponible", "Vendido", "DaÃ±ado", "Reservado"];
    private const string CamposInventario = "idinventario,producto_idproducto,imei,numero_serie,fecha_ingreso,estado";
    private readonly AppDbContext _context;

    public InventariosController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? busqueda, string? estado)
    {
        var inventarios = _context.inventarios
            .AsNoTracking()
            .Include(i => i.producto_idproductoNavigation)
            .AsQueryable();

        busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
        if (busqueda is not null)
        {
            inventarios = inventarios.Where(i =>
                i.producto_idproductoNavigation.nombre.Contains(busqueda) ||
                (i.imei != null && i.imei.Contains(busqueda)) ||
                (i.numero_serie != null && i.numero_serie.Contains(busqueda)));
        }

        if (!string.IsNullOrWhiteSpace(estado) && EstadosValidos.Contains(estado, StringComparer.Ordinal))
        {
            inventarios = inventarios.Where(i => i.estado == estado);
        }
        else
        {
            estado = null;
        }

        ViewBag.Busqueda = busqueda;
        ViewBag.Estado = estado;
        ViewBag.Estados = new SelectList(EstadosValidos, estado);

        var resultados = await inventarios
            .OrderBy(i => i.producto_idproductoNavigation.nombre)
            .ThenBy(i => i.idinventario)
            .ToListAsync();

        return View(resultados);
    }

    public async Task<IActionResult> Create()
    {
        await CargarProductosAsync();
        return View(new inventario
        {
            fecha_ingreso = DateTime.Now,
            estado = "Disponible"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("producto_idproducto,imei,numero_serie,fecha_ingreso")] inventario inventario)
    {
        // La navegaciÃ³n se resuelve por la FK en EF; no forma parte del formulario.
        ModelState.Remove(nameof(inventario.producto_idproductoNavigation));
        Normalizar(inventario);
        inventario.estado = "Disponible";
        await ValidarInventarioAsync(inventario);

        if (!ModelState.IsValid)
        {
            await CargarProductosAsync(inventario.producto_idproducto);
            return View(inventario);
        }

        _context.inventarios.Add(inventario);
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "La unidad de inventario se registrÃ³ correctamente como Disponible.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var inventario = await _context.inventarios
            .Include(i => i.detalle_venta)
            .Include(i => i.producto_idproductoNavigation)
            .FirstOrDefaultAsync(i => i.idinventario == id.Value);
        if (inventario is null) return NotFound();

        await CargarProductosAsync(inventario.producto_idproducto);
        ViewBag.TieneVenta = inventario.detalle_venta is not null;
        return View(inventario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(CamposInventario)] inventario inventario)
    {
        ModelState.Remove(nameof(inventario.producto_idproductoNavigation));
        if (id != inventario.idinventario) return NotFound();

        var existente = await _context.inventarios
            .Include(i => i.detalle_venta)
            .Include(i => i.producto_idproductoNavigation)
            .FirstOrDefaultAsync(i => i.idinventario == id);
        if (existente is null) return NotFound();

        Normalizar(inventario);
        await ValidarInventarioAsync(inventario, id);

        if (existente.detalle_venta is not null)
        {
            if (!string.Equals(inventario.estado, "Vendido", StringComparison.Ordinal))
                ModelState.AddModelError(nameof(inventario.estado), "La unidad tiene una venta asociada y debe conservar el estado Vendido.");

            if (inventario.producto_idproducto != existente.producto_idproducto ||
                inventario.imei != existente.imei ||
                inventario.numero_serie != existente.numero_serie ||
                inventario.fecha_ingreso != existente.fecha_ingreso)
            {
                ModelState.AddModelError(string.Empty, "La unidad tiene una venta asociada y sus datos no se pueden modificar.");
            }
        }
        else if (!string.Equals(existente.estado, "Vendido", StringComparison.Ordinal) &&
                 string.Equals(inventario.estado, "Vendido", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(inventario.estado), "El estado Vendido se asigna al registrar una venta.");
        }

        if (!ModelState.IsValid)
        {
            await CargarProductosAsync(inventario.producto_idproducto);
            ViewBag.TieneVenta = existente.detalle_venta is not null;
            inventario.producto_idproductoNavigation = existente.producto_idproductoNavigation;
            return View(inventario);
        }

        existente.producto_idproducto = inventario.producto_idproducto;
        existente.imei = inventario.imei;
        existente.numero_serie = inventario.numero_serie;
        existente.fecha_ingreso = inventario.fecha_ingreso;
        existente.estado = inventario.estado;

        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "La unidad de inventario se actualizÃ³ correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var inventario = await _context.inventarios
            .AsNoTracking()
            .Include(i => i.producto_idproductoNavigation)
            .Include(i => i.detalle_venta)
            .FirstOrDefaultAsync(i => i.idinventario == id.Value);
        return inventario is null ? NotFound() : View(inventario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var inventario = await _context.inventarios.FirstOrDefaultAsync(i => i.idinventario == id);
        if (inventario is null) return NotFound();
        if (await _context.detalle_ventas.AnyAsync(d => d.inventario_idinventario == id))
        {
            TempData["Error"] = "Una unidad vendida no puede marcarse como dañada desde esta acción.";
            return RedirectToAction(nameof(Index));
        }
        inventario.estado = "Dañado";
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Unidad marcada como Dañada y conservada en el historial.";
        return RedirectToAction(nameof(Index));
    }
    private async Task ValidarInventarioAsync(inventario inventario, int? idActual = null)
    {
        var productoActivo = await _context.productos.AnyAsync(p =>
            p.idproducto == inventario.producto_idproducto && p.activo);
        var conservaProductoInactivo = idActual.HasValue && await _context.inventarios.AnyAsync(i =>
            i.idinventario == idActual.Value && i.producto_idproducto == inventario.producto_idproducto);
        if (!productoActivo && !conservaProductoInactivo)
            ModelState.AddModelError(nameof(inventario.producto_idproducto), "Selecciona un producto activo.");

        if (inventario.imei?.Length > 30)
            ModelState.AddModelError(nameof(inventario.imei), "El IMEI no puede superar 30 caracteres.");
        if (inventario.numero_serie?.Length > 150)
            ModelState.AddModelError(nameof(inventario.numero_serie), "El nÃºmero de serie no puede superar 150 caracteres.");
        if (inventario.fecha_ingreso is null)
            ModelState.AddModelError(nameof(inventario.fecha_ingreso), "La fecha de ingreso es obligatoria.");
        if (inventario.estado is null || !EstadosValidos.Contains(inventario.estado, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(inventario.estado), "Selecciona un estado permitido.");

        if (inventario.imei is not null && await _context.inventarios.AnyAsync(i =>
                i.imei == inventario.imei && (!idActual.HasValue || i.idinventario != idActual.Value)))
        {
            ModelState.AddModelError(nameof(inventario.imei), "Ya existe una unidad con ese IMEI.");
        }

        if (inventario.numero_serie is not null && await _context.inventarios.AnyAsync(i =>
                i.numero_serie == inventario.numero_serie && (!idActual.HasValue || i.idinventario != idActual.Value)))
        {
            ModelState.AddModelError(nameof(inventario.numero_serie), "Ya existe una unidad con ese nÃºmero de serie.");
        }
    }

    private static void Normalizar(inventario inventario)
    {
        inventario.imei = TextoOpcional(inventario.imei);
        inventario.numero_serie = TextoOpcional(inventario.numero_serie);
    }

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task CargarProductosAsync(int? seleccionado = null)
    {
        var productos = await _context.productos
            .AsNoTracking()
            .Where(p => p.activo || (seleccionado.HasValue && p.idproducto == seleccionado.Value))
            .OrderBy(p => p.nombre)
            .ToListAsync();

        ViewBag.Productos = new SelectList(productos, "idproducto", "nombre", seleccionado);
        ViewBag.HayProductos = productos.Count > 0;
        ViewBag.Estados = new SelectList(EstadosValidos);
    }
}

