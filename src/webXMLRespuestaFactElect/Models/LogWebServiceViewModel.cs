namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Una fila del historial devuelto por `Get_LogWebService` (F-6). Reemplaza la
/// extracción anterior de un único `RespuestaXML`: el SP puede devolver varias
/// llamadas de webservice asociadas a los mismos criterios de búsqueda, que ahora se
/// muestran en un grid (columnas FechaHoraLog / MetodoWs / RespuestaXML).
/// </summary>
public sealed class LogWebServiceViewModel
{
    public DateTime? FechaHoraLog { get; init; }
    public string MetodoWs { get; init; } = string.Empty;
    public string RespuestaXml { get; init; } = string.Empty;
}
