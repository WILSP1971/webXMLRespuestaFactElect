using System.ComponentModel.DataAnnotations;

namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Parametros de entrada para el botón Buscar (F-6), enviados por AJAX/fetch al
/// controlador. Corresponden 1:1 a los 4 parametros de Get_LogWebService.
/// Ejemplo real: EXEC Get_LogWebService '07','FA','33',185138
/// </summary>
public sealed class BuscarXmlRequest
{
    [Required]
    public string Empresa { get; init; } = string.Empty;

    [Required]
    public string TipoDoc { get; init; } = string.Empty;

    [Required]
    public string Prefijo { get; init; } = string.Empty;

    [Required]
    public string NoDocumento { get; init; } = string.Empty;
}
