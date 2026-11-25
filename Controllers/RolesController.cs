using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;
using SistemaInventario.ViewModels;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class RolesController : Controller
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= "activos";
        var consulta = _context.roles.AsNoTracking();
        if (estado == "activos") consulta = consulta.Where(r => r.activo);
        else if (estado == "inactivos") consulta = consulta.Where(r => !r.activo);
        var roles = await consulta
            .OrderBy(r => r.nombre)
            .ThenBy(r => r.idrol)
            .Select(r => new RolListadoViewModel
            {
                IdRol = r.idrol,
                Nombre = r.nombre,
                Descripcion = r.descripcion,
                UsuariosAsignados = r.usuarios.Count,
                Activo = r.activo
            })
            .ToListAsync();

        ViewBag.EstadoFiltro = estado;
        return View(roles);
    }

    public IActionResult Create() => View(new RolFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RolFormViewModel model)
    {
        Normalizar(model);
        await ValidarNombreAsync(model.Nombre);
        if (!ModelState.IsValid) return View(model);

        var rol = new rol
        {
            nombre = model.Nombre,
            descripcion = model.Descripcion
        };
        _context.roles.Add(rol);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "El rol se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Nombre), "No se pudo guardar el rol. Verifica que el nombre no esté repetido.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var model = await _context.roles.AsNoTracking()
            .Where(r => r.idrol == id.Value)
            .Select(r => new RolFormViewModel
            {
                IdRol = r.idrol,
                Nombre = r.nombre,
                Descripcion = r.descripcion
            })
            .FirstOrDefaultAsync();

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RolFormViewModel model)
    {
        if (id != model.IdRol) return NotFound();

        Normalizar(model);
        await ValidarNombreAsync(model.Nombre, id);
        if (!ModelState.IsValid) return View(model);

        var rol = await _context.roles.FirstOrDefaultAsync(r => r.idrol == id);
        if (rol is null) return NotFound();

        if ((rol.nombre == "Administrador" || rol.nombre == "Vendedor") && rol.nombre != model.Nombre)
        {
            ModelState.AddModelError(nameof(model.Nombre), "Los nombres de los roles de acceso Administrador y Vendedor son necesarios para aplicar sus permisos.");
            return View(model);
        }

        rol.nombre = model.Nombre;
        rol.descripcion = model.Descripcion;
        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "El rol se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Nombre), "No se pudieron guardar los cambios. Verifica que el nombre no esté repetido.");
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var rol = await _context.roles.AsNoTracking()
            .Where(r => r.idrol == id.Value)
            .Select(r => new RolListadoViewModel
            {
                IdRol = r.idrol,
                Nombre = r.nombre,
                Descripcion = r.descripcion,
                UsuariosAsignados = r.usuarios.Count,
                Activo = r.activo
            })
            .FirstOrDefaultAsync();

        return rol is null ? NotFound() : View(rol);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var model = await _context.roles.AsNoTracking()
            .Where(r => r.idrol == id.Value)
            .Select(r => new RolEliminarViewModel
            {
                IdRol = r.idrol,
                Nombre = r.nombre,
                Descripcion = r.descripcion,
                UsuariosAsignados = r.usuarios.Count
            })
            .FirstOrDefaultAsync();

        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rol = await _context.roles.FirstOrDefaultAsync(r => r.idrol == id);
        if (rol is null) return NotFound();

        if (rol.nombre == "Administrador")
        {
            TempData["Error"] = "No se puede inhabilitar el rol Administrador porque dejaría el sistema sin acceso administrativo.";
            return RedirectToAction(nameof(Index));
        }

        rol.activo = false;
        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Rol inhabilitado. Se conservaron los usuarios e historial asociados.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "No se pudo inhabilitar el rol.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var rol = await _context.roles.FirstOrDefaultAsync(r => r.idrol == id);
        if (rol is null) return NotFound();
        rol.activo = true;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Rol activado nuevamente.";
        return RedirectToAction(nameof(Index), new { estado = "inactivos" });
    }

    private async Task ValidarNombreAsync(string? nombre, int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return;

        if (await _context.roles.AnyAsync(r => r.nombre == nombre &&
                (!idActual.HasValue || r.idrol != idActual.Value)))
        {
            ModelState.AddModelError(nameof(RolFormViewModel.Nombre), "Ya existe un rol con ese nombre.");
        }
    }

    private static void Normalizar(RolFormViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
    }
}
