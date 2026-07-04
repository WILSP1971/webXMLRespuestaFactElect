using webXMLRespuestaFactElect.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC con vistas Razor.
builder.Services.AddControllersWithViews();

// Capa de acceso a datos de solo lectura (Stored Procedures) - CHECKPOINT C5.
builder.Services.Configure<FactElectronicaDbOptions>(
    builder.Configuration.GetSection(FactElectronicaDbOptions.SeccionConfiguracion));
builder.Services.AddScoped<IFactElectronicaRepository, FactElectronicaRepository>();

var app = builder.Build();

// PathBase DEBE ir PRIMERO (antes de cualquier otro middleware)
app.UsePathBase(builder.Configuration["AppPathBase"] ?? "/LogWebServiceFactElectronica");

// PAGINA DE EXCEPCIONES DE DEVELOPER
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

// NO hay autenticación/autorización (ya se eliminó)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
