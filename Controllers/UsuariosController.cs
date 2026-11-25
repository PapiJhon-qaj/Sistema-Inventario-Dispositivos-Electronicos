using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;
using SistemaInventario.ViewModels;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private static readonly string[] EstadosValidos = ["Activo", "Inactivo"];
    private const int PasswordMinima = 8;
    private const int PasswordMaxima = 128;
    private readonly AppDbContext _context;
    private readonly PasswordHasher<usuario> _passwordHasher = new();

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= "Activo";
        var consulta = _context.usuarios.AsNoTracking();
        if (estado == "Activo" || estado == "Inactivo") consulta = consulta.Where(u => u.estado == estado);
        var usuarios = await consulta
            .OrderBy(u => u.nombre)
            .ThenBy(u => u.apellido)
            .ThenBy(u => u.idusuario)
            .Select(u => new UsuarioListadoViewModel
            {
                IdUsuario = u.idusuario,
                NombreCompleto = u.nombre + " " + u.apellido,
                Correo = u.correo,
                Estado = u.estado,
                Rol = u.rol_idrolNavigation.nombre,
                Ventas = u.venta.Count,
                Compras = u.compras.Count
            })
            .ToListAsync();

        ViewBag.EstadoFiltro = estado;
        return View(usuarios);
    }

    public async Task<IActionResult> Create()
    {
        var model = new UsuarioFormViewModel { Estado = "Activo" };
        await CargarRolesAsync(model.RolId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel model)
    {
        Normalizar(model);
        await ValidarUsuarioAsync(model, esNuevo: true);

        if (!ModelState.IsValid)
        {
            await CargarRolesAsync(model.RolId);
            return View(model);
        }

        var usuario = new usuario
        {
            nombre = model.Nombre,
            apellido = model.Apellido,
            correo = model.Correo,
            estado = model.Estado,
            rol_idrol = model.RolId!.Value
        };
        usuario.password = _passwordHasher.HashPassword(usuario, model.Password!);

        _context.usuarios.Add(usuario);
        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "El usuario se registró correctamente con contraseña protegida.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Correo), "No se pudo guardar el usuario. Verifica que el correo no esté registrado y que el rol siga existiendo.");
            await CargarRolesAsync(model.RolId);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var model = await _context.usuarios.AsNoTracking()
            .Where(u => u.idusuario == id.Value)
            .Select(u => new UsuarioFormViewModel
            {
                IdUsuario = u.idusuario,
                Nombre = u.nombre,
                Apellido = u.apellido,
                Correo = u.correo,
                Estado = u.estado ?? "Inactivo",
                RolId = u.rol_idrol
            })
            .FirstOrDefaultAsync();

        if (model is null) return NotFound();
        await CargarRolesAsync(model.RolId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioFormViewModel model)
    {
        if (id != model.IdUsuario) return NotFound();

        Normalizar(model);
        await ValidarUsuarioAsync(model, esNuevo: false, idActual: id);
        if (!ModelState.IsValid)
        {
            await CargarRolesAsync(model.RolId);
            return View(model);
        }

        var usuario = await _context.usuarios.FirstOrDefaultAsync(u => u.idusuario == id);
        if (usuario is null) return NotFound();

        if ((model.Estado == "Inactivo" || model.RolId != usuario.rol_idrol) && usuario.estado == "Activo" &&
            usuario.rol_idrolNavigation.nombre == "Administrador" &&
            await _context.usuarios.CountAsync(u => u.estado == "Activo" && u.rol_idrolNavigation.nombre == "Administrador") <= 1)
        {
            ModelState.AddModelError(nameof(model.Estado), "No se puede inhabilitar la última cuenta Administrador activa.");
            await CargarRolesAsync(model.RolId);
            return View(model);
        }

        usuario.nombre = model.Nombre;
        usuario.apellido = model.Apellido;
        usuario.correo = model.Correo;
        usuario.estado = model.Estado;
        usuario.rol_idrol = model.RolId!.Value;
        if (!string.IsNullOrWhiteSpace(model.Password))
            usuario.password = _passwordHasher.HashPassword(usuario, model.Password);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Los datos del usuario se actualizaron correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Correo), "No se pudieron guardar los cambios. Verifica el correo y el rol seleccionados.");
            await CargarRolesAsync(model.RolId);
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var usuario = await _context.usuarios.AsNoTracking()
            .Where(u => u.idusuario == id.Value)
            .Select(u => new UsuarioDetalleViewModel
            {
                IdUsuario = u.idusuario,
                Nombre = u.nombre,
                Apellido = u.apellido,
                Correo = u.correo,
                Estado = u.estado,
                Rol = u.rol_idrolNavigation.nombre,
                Ventas = u.venta.Count,
                Compras = u.compras.Count
            })
            .FirstOrDefaultAsync();

        return usuario is null ? NotFound() : View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _context.usuarios.FirstOrDefaultAsync(u => u.idusuario == id);
        if (usuario is null) return NotFound();

        if (usuario.estado == "Activo" && usuario.rol_idrolNavigation.nombre == "Administrador" &&
            await _context.usuarios.CountAsync(u => u.estado == "Activo" && u.rol_idrolNavigation.nombre == "Administrador") <= 1)
        {
            TempData["Error"] = "No se puede inhabilitar la última cuenta Administrador activa.";
            return RedirectToAction(nameof(Index));
        }

        if (usuario.estado == "Activo")
            usuario.estado = "Inactivo";
        else if (usuario.estado == "Inactivo")
            usuario.estado = "Activo";
        else
        {
            TempData["Error"] = "El estado guardado no coincide con los valores permitidos (Activo/Inactivo). No se hicieron cambios.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"El usuario se marcó como {usuario.estado}.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "No se pudo cambiar el estado del usuario.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var model = await CargarUsuarioEliminarAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var usuario = await _context.usuarios.FirstOrDefaultAsync(u => u.idusuario == id);
        if (usuario is null) return NotFound();
        if (usuario.estado == "Activo" && usuario.rol_idrolNavigation.nombre == "Administrador" &&
            await _context.usuarios.CountAsync(u => u.estado == "Activo" && u.rol_idrolNavigation.nombre == "Administrador") <= 1)
        {
            TempData["Error"] = "No se puede inhabilitar la última cuenta Administrador activa.";
            return RedirectToAction(nameof(Index));
        }
        usuario.estado = "Inactivo";
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Usuario inhabilitado. Se conservaron sus compras, ventas e historial.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarUsuarioAsync(UsuarioFormViewModel model, bool esNuevo, int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre))
            ModelState.AddModelError(nameof(model.Nombre), "El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(model.Apellido))
            ModelState.AddModelError(nameof(model.Apellido), "El apellido es obligatorio.");
        if (string.IsNullOrWhiteSpace(model.Correo))
            ModelState.AddModelError(nameof(model.Correo), "El correo es obligatorio.");
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(model.Correo))
            ModelState.AddModelError(nameof(model.Correo), "Escribe un correo válido.");

        if (model.Estado is null || !EstadosValidos.Contains(model.Estado, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Estado), "Selecciona un estado permitido.");

        if (model.RolId is null || !await _context.roles.AnyAsync(r => r.idrol == model.RolId.Value && r.activo))
            ModelState.AddModelError(nameof(model.RolId), "Selecciona un rol activo existente.");

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            if (esNuevo)
                ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria para crear un usuario.");
        }
        else if (model.Password.Length < PasswordMinima || model.Password.Length > PasswordMaxima)
        {
            ModelState.AddModelError(nameof(model.Password), $"La contraseña debe tener entre {PasswordMinima} y {PasswordMaxima} caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(model.Correo) && await _context.usuarios.AnyAsync(u =>
                u.correo == model.Correo && (!idActual.HasValue || u.idusuario != idActual.Value)))
        {
            ModelState.AddModelError(nameof(model.Correo), "Ya existe un usuario con ese correo.");
        }
    }

    private static void Normalizar(UsuarioFormViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Apellido = model.Apellido?.Trim() ?? string.Empty;
        model.Correo = model.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
        model.Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password;
    }

    private async Task CargarRolesAsync(int? seleccionado = null)
    {
        var roles = await _context.roles.AsNoTracking()
            .Where(r => r.activo)
            .OrderBy(r => r.nombre)
            .Select(r => new { r.idrol, r.nombre })
            .ToListAsync();

        ViewBag.Roles = new SelectList(roles, "idrol", "nombre", seleccionado);
        ViewBag.HayRoles = roles.Count > 0;
    }

    private Task<UsuarioEliminarViewModel?> CargarUsuarioEliminarAsync(int id) => _context.usuarios.AsNoTracking()
        .Where(u => u.idusuario == id)
        .Select(u => new UsuarioEliminarViewModel
        {
            IdUsuario = u.idusuario,
            NombreCompleto = u.nombre + " " + u.apellido,
            Correo = u.correo,
            Estado = u.estado,
            Rol = u.rol_idrolNavigation.nombre,
            Ventas = u.venta.Count,
            Compras = u.compras.Count
        })
        .FirstOrDefaultAsync();
}
