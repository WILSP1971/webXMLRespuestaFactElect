using Microsoft.AspNetCore.Mvc;

namespace webXMLRespuestaFactElect.Controllers;

/// <summary>
/// Controlador minimo: redirige la raiz hacia la unica vista funcional de esta SPEC
/// ("XML Respuesta Dian") y ofrece la pagina de error generica (sin stack trace,
/// NF-2) usada por el middleware UseExceptionHandler en produccion.
/// </summary>
public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "XmlRespuestaDian");

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
