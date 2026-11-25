using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaInventario.Data;

namespace SistemaInventario.Controllers
{
[Authorize(Roles = "Administrador")]
public class PruebaController : Controller
    {
        private readonly AppDbContext _context;

        public PruebaController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.ConexionCorrecta = await _context.Database.CanConnectAsync();
            }
            catch (Exception)
            {
                ViewBag.ConexionCorrecta = false;
            }

            return View();
        }
    }
}
