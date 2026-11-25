using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Data;
using SistemaInventario.Models;
using SistemaInventario.ViewModels;

namespace SistemaInventario.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<usuario> _passwordHasher = new();

    public AccountController(AppDbContext context) => _context = context;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.Correo = model.Correo?.Trim() ?? string.Empty;
        if (!ModelState.IsValid) return View(model);

        var usuario = await _context.usuarios
            .AsNoTracking()
            .Include(u => u.rol_idrolNavigation)
            .FirstOrDefaultAsync(u => u.correo == model.Correo);

        var credencialesValidas = usuario is not null &&
            usuario.estado == "Activo" && usuario.rol_idrolNavigation.activo &&
            VerificarPassword(usuario, model.Password);

        if (!credencialesValidas || usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos, o la cuenta está inactiva.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.idusuario.ToString()),
            new(ClaimTypes.Name, $"{usuario.nombre} {usuario.apellido}".Trim()),
            new(ClaimTypes.Email, usuario.correo),
            new(ClaimTypes.Role, usuario.rol_idrolNavigation.nombre)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

        return RedirectToAction("Index", "Home");
    }

    private bool VerificarPassword(usuario usuario, string password)
    {
        try
        {
            return _passwordHasher.VerifyHashedPassword(usuario, usuario.password, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Las contraseñas antiguas en texto plano no se convierten ni se aceptan como hash.
            return false;
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
