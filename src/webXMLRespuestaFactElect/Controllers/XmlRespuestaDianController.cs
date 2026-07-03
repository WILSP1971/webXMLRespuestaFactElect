using Microsoft.AspNetCore.Mvc;
using webXMLRespuestaFactElect.Models;
using webXMLRespuestaFactElect.Services;

namespace webXMLRespuestaFactElect.Controllers;

/// <summary>
/// Controlador de la vista "XML Respuesta Dian" (unico foco funcional de SPEC-003).
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

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// F-1: dropdown Empresas -> GetEmpresas (sin parametros).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerEmpresas(CancellationToken ct)
    {
        var resultado = await _repositorio.ObtenerEmpresasAsync(ct);

        if (!resultado.Exitoso)
        {
            _logger.LogWarning("ObtenerEmpresas -> fallback 503: {Msg}", resultado.MensajeError);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensajeError = MensajeErrorEmpresas });
        }
        return Json(resultado.Valor);
    }

    /// <summary>
    /// F-3: dropdown Tipo de Documento -> Get_TipoDocumentosFactElect @codEmpresa.
    /// Acepta `codEmpresa` como query param (cascada Empresa -> TipoDoc).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTipoDocumentos([FromQuery(Name = "codEmpresa")] string? codEmpresa, CancellationToken ct)
    {
        _logger.LogInformation("ObtenerTipoDocumentos: codEmpresa='{Emp}'", codEmpresa ?? "(TODAS)");
        var resultado = await _repositorio.ObtenerTipoDocumentosAsync(codEmpresa, ct);

        if (!resultado.Exitoso)
        {
            _logger.LogWarning("ObtenerTipoDocumentos -> fallback 503: {Msg}", resultado.MensajeError);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensajeError = MensajeErrorTiposDocumento });
        }
        return Json(resultado.Valor);
    }

    /// <summary>
    /// F-6: Get_LogWebService @codEmpresa, @tipoDocumento, @prefijo, @noDocumento.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Buscar([FromBody] BuscarXmlRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Buscar: codEmpresa='{Emp}', tipoDocumento='{Td}', prefijo='{Pf}', noDocumento='{Nd}'",
            request?.Empresa, request?.TipoDoc, request?.Prefijo, request?.NoDocumento);

        if (!TryValidarYNormalizarNoDocumento(request, out var noDocumento))
        {
            _logger.LogWarning("Buscar: parametros invalidos");
            return BadRequest(BuscarXmlResponse.ConError(MensajeParametrosInvalidos));
        }

        var resultado = await _repositorio.ObtenerHistorialLogAsync(
            request.Empresa, request.TipoDoc, request.Prefijo, noDocumento, ct);

        if (!resultado.Exitoso)
        {
            _logger.LogWarning("Buscar -> fallback: {Msg}", resultado.MensajeError);
            return Json(BuscarXmlResponse.ConError(MensajeErrorBusqueda));
        }

        _logger.LogInformation("Buscar -> devolvio {N} filas", resultado.Valor!.Count);

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
