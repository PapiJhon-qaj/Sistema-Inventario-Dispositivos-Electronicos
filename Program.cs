
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SistemaInventario.Models;
using SistemaInventario.Data;

var builder = WebApplication.CreateBuilder(args);

// Evita errores del proveedor de eventos de Windows en ejecuciones desde VS Code.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Registrar MVC
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "SistemaInventario.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Events.OnValidatePrincipal = async context =>
        {
            var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = context.Principal?.FindFirstValue(ClaimTypes.Role);
            if (!int.TryParse(idClaim, out var idUsuario) || string.IsNullOrWhiteSpace(roleClaim))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var sigueActivo = await db.usuarios.AsNoTracking()
                .AnyAsync(u => u.idusuario == idUsuario && u.estado == "Activo" &&
                    u.rol_idrolNavigation.activo && u.rol_idrolNavigation.nombre == roleClaim);
            if (!sigueActivo)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Los formularios MVC necesitan claves persistentes para generar tokens antifalsificación.
// Guardarlas dentro del proyecto evita depender de una carpeta de usuario sin permisos.
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath);
var dataProtection = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
if (!builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi();
}

// Obtener la cadena de conexión
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

// Conectar Entity Framework con MariaDB de XAMPP
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(10, 4, 32))
    )
);

var app = builder.Build();

// Configuración de la aplicación
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
