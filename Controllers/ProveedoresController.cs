using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class ProveedoresController : Controller
{
    private const string CamposProveedor = "idproveedor,nit,nombre,direccion,telefono,correo";
    private readonly AppDbContext _context;

    public ProveedoresController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= "activos";
        var consulta = _context.proveedores.AsNoTracking();
        if (estado == "activos") consulta = consulta.Where(p => p.activo);
        else if (estado == "inactivos") consulta = consulta.Where(p => !p.activo);
        var proveedores = await consulta
            .AsNoTracking()
            .OrderBy(p => p.nombre)
            .ThenBy(p => p.idproveedor)
            .ToListAsync();

        ViewBag.EstadoFiltro = estado;
        return View(proveedores);
    }

    public IActionResult Create() => View(new proveedor());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CamposProveedor)] proveedor proveedor)
    {
        Normalizar(proveedor);
        await ValidarProveedorAsync(proveedor);
        if (!ModelState.IsValid) return View(proveedor);

        _context.proveedores.Add(proveedor);
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "El proveedor se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var proveedor = await _context.proveedores.FindAsync(id.Value);
        return proveedor is null ? NotFound() : View(proveedor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(CamposProveedor)] proveedor proveedor)
    {
        if (id != proveedor.idproveedor) return NotFound();

        Normalizar(proveedor);
        await ValidarProveedorAsync(proveedor, id);
        if (!ModelState.IsValid) return View(proveedor);

        var existente = await _context.proveedores.FirstOrDefaultAsync(p => p.idproveedor == id);
        if (existente is null) return NotFound();

        existente.nit = proveedor.nit;
        existente.nombre = proveedor.nombre;
        existente.direccion = proveedor.direccion;
        existente.telefono = proveedor.telefono;
        existente.correo = proveedor.correo;

        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "El proveedor se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var proveedor = await _context.proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.idproveedor == id.Value);
        return proveedor is null ? NotFound() : View(proveedor);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var proveedor = await _context.proveedores.FindAsync(id);
        if (proveedor is null) return NotFound();

        proveedor.activo = false;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Proveedor inhabilitado. Se conservaron sus compras e historial.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var proveedor = await _context.proveedores.FindAsync(id);
        if (proveedor is null) return NotFound();
        proveedor.activo = true;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Proveedor activado nuevamente.";
        return RedirectToAction(nameof(Index), new { estado = "inactivos" });
    }

    private async Task ValidarProveedorAsync(proveedor proveedor, int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(proveedor.nit))
            ModelState.AddModelError(nameof(proveedor.nit), "El NIT es obligatorio.");
        if (proveedor.nit?.Length > 30)
            ModelState.AddModelError(nameof(proveedor.nit), "El NIT no puede superar 30 caracteres.");
        if (string.IsNullOrWhiteSpace(proveedor.nombre))
            ModelState.AddModelError(nameof(proveedor.nombre), "El nombre es obligatorio.");
        if (proveedor.nombre.Length > 150)
            ModelState.AddModelError(nameof(proveedor.nombre), "El nombre no puede superar 150 caracteres.");
        if (proveedor.direccion?.Length > 200)
            ModelState.AddModelError(nameof(proveedor.direccion), "La dirección no puede superar 200 caracteres.");
        if (proveedor.telefono?.Length > 30)
            ModelState.AddModelError(nameof(proveedor.telefono), "El teléfono no puede superar 30 caracteres.");
        if (proveedor.correo?.Length > 120)
            ModelState.AddModelError(nameof(proveedor.correo), "El correo no puede superar 120 caracteres.");
        else if (proveedor.correo is not null && !new EmailAddressAttribute().IsValid(proveedor.correo))
            ModelState.AddModelError(nameof(proveedor.correo), "Escribe un correo electrónico válido.");

        if (!string.IsNullOrWhiteSpace(proveedor.nit) && await _context.proveedores.AnyAsync(p =>
                p.nit == proveedor.nit && (!idActual.HasValue || p.idproveedor != idActual.Value)))
        {
            ModelState.AddModelError(nameof(proveedor.nit), "Ya existe un proveedor con ese NIT.");
        }
    }

    private static void Normalizar(proveedor proveedor)
    {
        proveedor.nit = TextoObligatorio(proveedor.nit);
        proveedor.nombre = TextoObligatorio(proveedor.nombre);
        proveedor.direccion = TextoOpcional(proveedor.direccion);
        proveedor.telefono = TextoOpcional(proveedor.telefono);
        proveedor.correo = TextoOpcional(proveedor.correo);
    }

    private static string TextoObligatorio(string? valor) => valor?.Trim() ?? string.Empty;

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
