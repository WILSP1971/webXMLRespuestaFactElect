using Microsoft.AspNetCore.Mvc;
using webXMLRespuestaFactElect.Models;
using webXMLRespuestaFactElect.Services;

namespace webXMLRespuestaFactElect.Controllers;

/// <summary>
/// Controlador de la vista "XML Respuesta Dian" (unico foco funcional de SPEC-003).
/// Todas las acciones de datos son de solo lectura y delegan en
/// <see cref="IFactElectronicaRepository"/> (CHECKPOINT C5); no hay SQL de negocio
/// inline aqui. Los errores de infraestructura se traducen a mensajes controlados
/// (NF-2 / AC-C3), nunca se expone un stack trace al cliente.
/// </summary>
public sealed class XmlRespuestaDianController : Controller
{
    private readonly IFactElectronicaRepository _repositorio;
    private readonly ILogger<XmlRespuestaDianController> _logger;

    private const string MensajeErrorEmpresas = "No fue posible cargar las empresas. Intente de nuevo.";
    private const string MensajeErrorTiposDocumento = "No fue posible cargar los tipos de documento. Intente de nuevo.";
    private const string MensajeErrorBusqueda =
        "No fue posible completar la consulta. Intente nuevamente en unos minutos.";
    private const string MensajeParametrosInvalidos =
        "Complete empresa, tipo de documento, prefijo y numero de documento validos.";

    public XmlRespuestaDianController(IFactElectronicaRepository repositorio, ILogger<XmlRespuestaDianController> logger)
    {
        _repositorio = repositorio;
        _logger = logger;
    }

    /// <summary>Carga la vista "XML Respuesta Dian" (F-1..F-8). Los dropdowns se pueblan via fetch.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>F-1: poblado del dropdown Empresas desde GetEmpresas.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerEmpresas(CancellationToken ct)
    {
        var resultado = await _repositorio.ObtenerEmpresasAsync(ct);

        if (!resultado.Exitoso)
        {
            _logger.LogWarning("Fallo al obtener empresas: {Mensaje}", resultado.MensajeError);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensajeError = MensajeErrorEmpresas });
        }

        return Json(resultado.Valor);
    }

    /// <summary>F-3: poblado del dropdown Tipo de Documento desde Get_TipoDocumentosFactElect.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTipoDocumentos(CancellationToken ct)
    {
        var resultado = await _repositorio.ObtenerTipoDocumentosAsync(ct);

        if (!resultado.Exitoso)
        {
            _logger.LogWarning("Fallo al obtener tipos de documento: {Mensaje}", resultado.MensajeError);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensajeError = MensajeErrorTiposDocumento });
        }

        return Json(resultado.Valor);
    }

    /// <summary>
    /// F-6: invoca Get_LogWebService con los 4 parametros y devuelve TODAS las filas
    /// del historial (FechaHoraLog, MetodoWs, RespuestaXML) para el grid de la vista,
    /// con el XML de cada fila ya formateado (F-7). Lista vacia = "sin resultados"
    /// (AC-6); `Error=true` = fallo controlado (AC-C3/NF-2).
    /// </summary>
    // Nota: sin [ValidateAntiForgeryToken] a proposito. La accion es de solo lectura
    // (no muta estado ni datos de negocio); la app ahora exige autenticacion Windows
    // integrada (revision de S-6, ver contexto/ESTADO.md), pero eso no cambia que el
    // CSRF token no aporte proteccion adicional a una lectura idempotente.
    [HttpPost]
    public async Task<IActionResult> Buscar([FromBody] BuscarXmlRequest request, CancellationToken ct)
    {
        if (!TryValidarYNormalizarNoDocumento(request, out var noDocumento))
        {
            return BadRequest(BuscarXmlResponse.ConError(MensajeParametrosInvalidos));
        }

        var resultado = await _repositorio.ObtenerHistorialLogAsync(
            request.Empresa, request.TipoDoc, request.Prefijo, noDocumento, ct);

        if (!resultado.Exitoso)
        {
            _logger.LogWarning("Fallo al ejecutar Get_LogWebService: {Mensaje}", resultado.MensajeError);
            return Json(BuscarXmlResponse.ConError(MensajeErrorBusqueda));
        }

        var registros = resultado.Valor!
            .Select(registro => new LogWebServiceViewModel
            {
                FechaHoraLog = registro.FechaHoraLog,
                MetodoWs = registro.MetodoWs,
                RespuestaXml = XmlFormatter.Formatear(registro.RespuestaXml)
            })
            .ToList();

        return Json(BuscarXmlResponse.ConRegistros(registros));
    }

    private static bool TryValidarYNormalizarNoDocumento(BuscarXmlRequest request, out long noDocumento)
    {
        noDocumento = 0;

        if (request is null
            || string.IsNullOrWhiteSpace(request.Empresa)
            || string.IsNullOrWhiteSpace(request.TipoDoc)
            || string.IsNullOrWhiteSpace(request.Prefijo)
            || string.IsNullOrWhiteSpace(request.NoDocumento))
        {
            return false;
        }

        return long.TryParse(request.NoDocumento, out noDocumento) && noDocumento >= 0;
    }
}
