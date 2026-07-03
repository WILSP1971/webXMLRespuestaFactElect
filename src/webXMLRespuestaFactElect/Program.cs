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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=XmlRespuestaDian}/{action=Index}/{id?}");

app.Run();
