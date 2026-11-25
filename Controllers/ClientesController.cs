using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;

namespace SistemaInventario.Controllers;

[Authorize(Roles = "Administrador,Vendedor")]
public class ClientesController : Controller
{
    private const string CamposCliente = "idcliente,documento,nombre,apellido,telefono,correo";
    private readonly AppDbContext _context;

    public ClientesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= "activos";
        var consulta = _context.clientes.AsNoTracking();
        if (estado == "activos") consulta = consulta.Where(c => c.activo);
        else if (estado == "inactivos") consulta = consulta.Where(c => !c.activo);
        var clientes = await consulta
            .AsNoTracking()
            .OrderBy(c => c.nombre)
            .ThenBy(c => c.apellido)
            .ThenBy(c => c.idcliente)
            .ToListAsync();

        ViewBag.EstadoFiltro = estado;
        return View(clientes);
    }

    [Authorize(Roles = "Vendedor")]
    public IActionResult Create() => View(new cliente());

    [HttpPost]
    [Authorize(Roles = "Vendedor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CamposCliente)] cliente cliente)
    {
        Normalizar(cliente);
        await ValidarClienteAsync(cliente);
        if (!ModelState.IsValid) return View(cliente);

        _context.clientes.Add(cliente);
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "El cliente se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var cliente = await _context.clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.idcliente == id.Value);
        return cliente is null ? NotFound() : View(cliente);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var cliente = await _context.clientes.FindAsync(id.Value);
        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(CamposCliente)] cliente cliente)
    {
        if (id != cliente.idcliente) return NotFound();

        Normalizar(cliente);
        await ValidarClienteAsync(cliente, id);
        if (!ModelState.IsValid) return View(cliente);

        var existente = await _context.clientes.FirstOrDefaultAsync(c => c.idcliente == id);
        if (existente is null) return NotFound();

        existente.documento = cliente.documento;
        existente.nombre = cliente.nombre;
        existente.apellido = cliente.apellido;
        existente.telefono = cliente.telefono;
        existente.correo = cliente.correo;

        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "El cliente se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var cliente = await _context.clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.idcliente == id.Value);
        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cliente = await _context.clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        cliente.activo = false;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Cliente inhabilitado. Se conservaron sus ventas e historial.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var cliente = await _context.clientes.FirstOrDefaultAsync(c => c.idcliente == id);
        if (cliente is null) return NotFound();
        cliente.activo = true;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Cliente activado nuevamente.";
        return RedirectToAction(nameof(Index), new { estado = "inactivos" });
    }

    private async Task ValidarClienteAsync(cliente cliente, int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(cliente.documento))
            ModelState.AddModelError(nameof(cliente.documento), "El documento es obligatorio.");
        if (cliente.documento?.Length > 30)
            ModelState.AddModelError(nameof(cliente.documento), "El documento no puede superar 30 caracteres.");
        if (string.IsNullOrWhiteSpace(cliente.nombre))
            ModelState.AddModelError(nameof(cliente.nombre), "El nombre es obligatorio.");
        if (cliente.nombre.Length > 100)
            ModelState.AddModelError(nameof(cliente.nombre), "El nombre no puede superar 100 caracteres.");
        if (string.IsNullOrWhiteSpace(cliente.apellido))
            ModelState.AddModelError(nameof(cliente.apellido), "El apellido es obligatorio.");
        if (cliente.apellido?.Length > 100)
            ModelState.AddModelError(nameof(cliente.apellido), "El apellido no puede superar 100 caracteres.");
        if (cliente.telefono?.Length > 30)
            ModelState.AddModelError(nameof(cliente.telefono), "El teléfono no puede superar 30 caracteres.");
        if (cliente.correo?.Length > 120)
            ModelState.AddModelError(nameof(cliente.correo), "El correo no puede superar 120 caracteres.");
        else if (cliente.correo is not null && !new EmailAddressAttribute().IsValid(cliente.correo))
            ModelState.AddModelError(nameof(cliente.correo), "Escribe un correo electrónico válido.");

        if (!string.IsNullOrWhiteSpace(cliente.documento) && await _context.clientes.AnyAsync(c =>
                c.documento == cliente.documento && (!idActual.HasValue || c.idcliente != idActual.Value)))
        {
            ModelState.AddModelError(nameof(cliente.documento), "Ya existe un cliente con ese documento.");
        }
    }

    private static void Normalizar(cliente cliente)
    {
        cliente.documento = TextoObligatorio(cliente.documento);
        cliente.nombre = TextoObligatorio(cliente.nombre);
        cliente.apellido = TextoObligatorio(cliente.apellido);
        cliente.telefono = TextoOpcional(cliente.telefono);
        cliente.correo = TextoOpcional(cliente.correo);
    }

    private static string TextoObligatorio(string? valor) => valor?.Trim() ?? string.Empty;

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
