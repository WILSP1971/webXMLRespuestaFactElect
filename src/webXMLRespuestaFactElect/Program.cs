using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using webXMLRespuestaFactElect.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC con vistas Razor.
builder.Services.AddControllersWithViews();

// Capa de acceso a datos de solo lectura (Stored Procedures) - CHECKPOINT C5.
builder.Services.Configure<FactElectronicaDbOptions>(
    builder.Configuration.GetSection(FactElectronicaDbOptions.SeccionConfiguracion));
builder.Services.AddScoped<IFactElectronicaRepository, FactElectronicaRepository>();

// Autenticacion Windows/AD integrada.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// PAGINA DE EXCEPCIONES DE DEVELOPER: muestra el stack trace real en el navegador
// cuando algo se rompe (en vez de un 500 mudo). Solo activo en Development.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
