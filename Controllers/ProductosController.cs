using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class ProductosController : Controller
{
    private const string CamposProducto = "idproducto,nombre,descripcion,marca,modelo,codigo_barras,precio_venta,stock_minimo,categoria_idcategoria";
    private readonly AppDbContext _context;
    private readonly ILogger<ProductosController> _logger;

    public ProductosController(AppDbContext context, ILogger<ProductosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? estado, string? ordenarPor, string? direccion)
    {
        estado ??= "activos";
        ordenarPor = new[] { "id", "nombre", "marca", "modelo", "categoria", "precio", "stock" }
            .Contains(ordenarPor) ? ordenarPor : "nombre";
        direccion = direccion == "desc" ? "desc" : "asc";
        var consulta = _context.productos.AsNoTracking();
        if (estado == "activos") consulta = consulta.Where(p => p.activo);
        else if (estado == "inactivos") consulta = consulta.Where(p => !p.activo);

        IOrderedQueryable<producto> ordenada = (ordenarPor, direccion) switch
        {
            ("id", "desc") => consulta.OrderByDescending(p => p.idproducto),
            ("id", _) => consulta.OrderBy(p => p.idproducto),
            ("marca", "desc") => consulta.OrderByDescending(p => p.marca).ThenBy(p => p.idproducto),
            ("marca", _) => consulta.OrderBy(p => p.marca).ThenBy(p => p.idproducto),
            ("modelo", "desc") => consulta.OrderByDescending(p => p.modelo).ThenBy(p => p.idproducto),
            ("modelo", _) => consulta.OrderBy(p => p.modelo).ThenBy(p => p.idproducto),
            ("categoria", "desc") => consulta.OrderByDescending(p => p.categoria_idcategoriaNavigation.nombre).ThenBy(p => p.idproducto),
            ("categoria", _) => consulta.OrderBy(p => p.categoria_idcategoriaNavigation.nombre).ThenBy(p => p.idproducto),
            ("precio", "desc") => consulta.OrderByDescending(p => p.precio_venta).ThenBy(p => p.idproducto),
            ("precio", _) => consulta.OrderBy(p => p.precio_venta).ThenBy(p => p.idproducto),
            ("stock", "desc") => consulta.OrderByDescending(p => p.stock_minimo).ThenBy(p => p.idproducto),
            ("stock", _) => consulta.OrderBy(p => p.stock_minimo).ThenBy(p => p.idproducto),
            ("nombre", "desc") => consulta.OrderByDescending(p => p.nombre).ThenBy(p => p.idproducto),
            _ => consulta.OrderBy(p => p.nombre).ThenBy(p => p.idproducto)
        };

        var productos = await ordenada
            .Include(p => p.categoria_idcategoriaNavigation)
            .ToListAsync();

        ViewBag.EstadoFiltro = estado;
        ViewBag.OrdenarPor = ordenarPor;
        ViewBag.Direccion = direccion;
        return View(productos);
    }

    public async Task<IActionResult> Create()
    {
        await CargarCategoriasAsync();
        return View(new producto { stock_minimo = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CamposProducto)] producto producto)
    {
        // La vista envía la FK; EF carga la navegación, que no forma parte del formulario.
        ModelState.Remove(nameof(producto.categoria_idcategoriaNavigation));
        Normalizar(producto);
        await ValidarProductoAsync(producto);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validación rechazó producto. Errores: {Errores}", string.Join("; ",
                ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))));
            await CargarCategoriasAsync(producto.categoria_idcategoria);
            return View(producto);
        }

        _context.productos.Add(producto);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "No se pudo guardar el producto {Nombre}", producto.nombre);
            ModelState.AddModelError(string.Empty,
                "No se pudo guardar el producto. Verifica que el código de barras no esté repetido y que la categoría siga activa.");
            await CargarCategoriasAsync(producto.categoria_idcategoria);
            return View(producto);
        }

        TempData["Mensaje"] = "El producto se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var producto = await _context.productos.FindAsync(id.Value);
        if (producto is null) return NotFound();
        await CargarCategoriasAsync(producto.categoria_idcategoria);
        return View(producto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(CamposProducto)] producto producto)
    {
        if (id != producto.idproducto) return NotFound();

        ModelState.Remove(nameof(producto.categoria_idcategoriaNavigation));
        Normalizar(producto);
        await ValidarProductoAsync(producto, id);
        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(producto.categoria_idcategoria);
            return View(producto);
        }

        var existente = await _context.productos.FirstOrDefaultAsync(p => p.idproducto == id);
        if (existente is null) return NotFound();

        existente.nombre = producto.nombre;
        existente.descripcion = producto.descripcion;
        existente.marca = producto.marca;
        existente.modelo = producto.modelo;
        existente.codigo_barras = producto.codigo_barras;
        existente.precio_venta = producto.precio_venta;
        existente.stock_minimo = producto.stock_minimo;
        existente.categoria_idcategoria = producto.categoria_idcategoria;

        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "El producto se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var producto = await _context.productos
            .AsNoTracking()
            .Include(p => p.categoria_idcategoriaNavigation)
            .FirstOrDefaultAsync(p => p.idproducto == id.Value);
        return producto is null ? NotFound() : View(producto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var producto = await _context.productos.FindAsync(id);
        if (producto is null) return NotFound();

        producto.activo = false;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Producto inhabilitado. Se conservaron su inventario y su historial.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var producto = await _context.productos.FindAsync(id);
        if (producto is null) return NotFound();
        producto.activo = true;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Producto activado nuevamente.";
        return RedirectToAction(nameof(Index), new { estado = "inactivos" });
    }

    private async Task ValidarProductoAsync(producto producto, int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(producto.nombre))
            ModelState.AddModelError(nameof(producto.nombre), "El nombre es obligatorio.");
        if (producto.nombre.Length > 150)
            ModelState.AddModelError(nameof(producto.nombre), "El nombre no puede superar 150 caracteres.");
        if (producto.marca?.Length > 100)
            ModelState.AddModelError(nameof(producto.marca), "La marca no puede superar 100 caracteres.");
        if (producto.modelo?.Length > 100)
            ModelState.AddModelError(nameof(producto.modelo), "El modelo no puede superar 100 caracteres.");
        if (producto.codigo_barras?.Length > 100)
            ModelState.AddModelError(nameof(producto.codigo_barras), "El código de barras no puede superar 100 caracteres.");
        if (producto.precio_venta < 0 || producto.precio_venta > 99999999.99m)
            ModelState.AddModelError(nameof(producto.precio_venta), "El precio debe estar entre 0 y 99.999.999,99.");
        if (producto.stock_minimo < 0)
            ModelState.AddModelError(nameof(producto.stock_minimo), "El stock mínimo no puede ser negativo.");

        var categoriaActiva = await _context.categorias
            .AnyAsync(c => c.idcategoria == producto.categoria_idcategoria && c.activo);
        var conservaCategoriaInactiva = idActual.HasValue && await _context.productos.AnyAsync(p =>
            p.idproducto == idActual.Value && p.categoria_idcategoria == producto.categoria_idcategoria);
        if (!categoriaActiva && !conservaCategoriaInactiva)
            ModelState.AddModelError(nameof(producto.categoria_idcategoria), "Selecciona una categoría activa.");

        if (producto.codigo_barras is not null && await _context.productos.AnyAsync(p =>
                p.codigo_barras == producto.codigo_barras &&
                (!idActual.HasValue || p.idproducto != idActual.Value)))
        {
            ModelState.AddModelError(nameof(producto.codigo_barras), "Ya existe un producto con ese código de barras.");
        }
    }

    private static void Normalizar(producto producto)
    {
        producto.nombre = producto.nombre?.Trim() ?? string.Empty;
        producto.descripcion = TextoOpcional(producto.descripcion);
        producto.marca = TextoOpcional(producto.marca);
        producto.modelo = TextoOpcional(producto.modelo);
        producto.codigo_barras = TextoOpcional(producto.codigo_barras);
    }

    private static string? TextoOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private async Task CargarCategoriasAsync(int? seleccionada = null)
    {
        var categorias = await _context.categorias
            .AsNoTracking()
            .Where(c => c.activo || (seleccionada.HasValue && c.idcategoria == seleccionada.Value))
            .OrderBy(c => c.nombre)
            .ToListAsync();

        ViewBag.Categorias = new SelectList(categorias, "idcategoria", "nombre", seleccionada);
        ViewBag.HayCategorias = categorias.Count > 0;
    }
}
