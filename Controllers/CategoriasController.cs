
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;

namespace SistemaInventario.Controllers
{
[Authorize(Roles = "Administrador")]
    public class CategoriasController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriasController(AppDbContext context)
        {
            _context = context;
        }

        // LISTAR CATEGORÍAS
        public async Task<IActionResult> Index(string? estado)
        {
            estado ??= "activos";
            var consulta = _context.categorias.AsNoTracking();
            if (estado == "activos") consulta = consulta.Where(c => c.activo);
            else if (estado == "inactivos") consulta = consulta.Where(c => !c.activo);
            var categorias = await consulta
                .AsNoTracking()
                .OrderBy(c => c.idcategoria)
                .ToListAsync();
            ViewBag.EstadoFiltro = estado;
            return View(categorias);
        }

        // MOSTRAR FORMULARIO DE REGISTRO
        public IActionResult Create()
        {
            return View();
        }

        // GUARDAR CATEGORÍA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(categoria categoria)
        {
            categoria.activo = true;
            categoria.nombre = categoria.nombre?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(categoria.nombre))
            {
                ModelState.AddModelError("nombre", "El nombre es obligatorio.");
            }

            if (categoria.nombre?.Length > 100)
            {
                ModelState.AddModelError("nombre", "El nombre no puede superar 100 caracteres.");
            }

            if (categoria.descripcion?.Length > 255)
            {
                ModelState.AddModelError("descripcion", "La descripción no puede superar 255 caracteres.");
            }

            if (!string.IsNullOrEmpty(categoria.nombre) &&
                await _context.categorias.AnyAsync(c => c.nombre == categoria.nombre))
            {
                ModelState.AddModelError("nombre", "Ya existe una categoría con ese nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            _context.categorias.Add(categoria);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Categoría registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // MOSTRAR FORMULARIO DE EDICIÓN
        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await _context.categorias.FindAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // ACTUALIZAR CATEGORÍA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, categoria categoria)
        {
            if (id != categoria.idcategoria)
            {
                return NotFound();
            }

            categoria.nombre = categoria.nombre?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(categoria.nombre))
            {
                ModelState.AddModelError("nombre", "El nombre es obligatorio.");
            }

            if (categoria.nombre?.Length > 100)
            {
                ModelState.AddModelError("nombre", "El nombre no puede superar 100 caracteres.");
            }

            if (categoria.descripcion?.Length > 255)
            {
                ModelState.AddModelError("descripcion", "La descripción no puede superar 255 caracteres.");
            }

            bool nombreDuplicado = await _context.categorias.AnyAsync(
                c => c.nombre == categoria.nombre &&
                     c.idcategoria != categoria.idcategoria
            );

            if (nombreDuplicado)
            {
                ModelState.AddModelError("nombre", "Ya existe otra categoría con ese nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            var categoriaExistente = await _context.categorias.FindAsync(id);

            if (categoriaExistente == null)
            {
                return NotFound();
            }

            categoriaExistente.nombre = categoria.nombre!;
            categoriaExistente.descripcion = categoria.descripcion;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Categoría actualizada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // MOSTRAR CONFIRMACIÓN DE ELIMINACIÓN
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _context.categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.idcategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // ELIMINAR CATEGORÍA
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.categorias.FindAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            try
            {
                categoria.activo = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Categoría inhabilitada correctamente. El registro y sus relaciones se conservaron.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo inhabilitar la categoría.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            var categoria = await _context.categorias.FindAsync(id);
            if (categoria is null) return NotFound();
            categoria.activo = true;
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Categoría activada nuevamente.";
            return RedirectToAction(nameof(Index), new { estado = "inactivos" });
        }
    }
}
