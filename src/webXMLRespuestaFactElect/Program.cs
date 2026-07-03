using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using webXMLRespuestaFactElect.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC con vistas Razor.
builder.Services.AddControllersWithViews();

// Capa de acceso a datos de solo lectura (Stored Procedures) - CHECKPOINT C5.
// La connection string se lee de configuracion (User Secrets en dev, variables de
// entorno en IIS) bajo la clave "ConnectionStrings:CadenaConexionDB". NUNCA se
// hardcodea aqui (CHECKPOINT C3).
builder.Services.Configure<FactElectronicaDbOptions>(
    builder.Configuration.GetSection(FactElectronicaDbOptions.SeccionConfiguracion));
builder.Services.AddScoped<IFactElectronicaRepository, FactElectronicaRepository>();

// Autenticacion Windows/AD integrada (revision de S-6: la app dejo de asumir "solo
// LAN sin login"; ver contexto/ESTADO.md). En IIS, Negotiate delega el handshake al
// modulo nativo de Windows Authentication (ver web.config); en Kestrel/dev usa SSPI
// directamente. FallbackPolicy exige usuario autenticado en TODA la app salvo que
// una accion se marque explicitamente [AllowAnonymous].
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=XmlRespuestaDian}/{action=Index}/{id?}");

app.Run();
