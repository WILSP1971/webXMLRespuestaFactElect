namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Respuesta JSON de la accion Buscar (F-6) consumida por fetch/AJAX desde la vista.
/// Exactamente uno de los tres casos aplica: encontrado=true (con Xml), encontrado=false
/// (sin resultados, AC-6), o error=true (fallo controlado, AC-C3/NF-2).
/// </summary>
public sealed class BuscarXmlResponse
{
    public bool Encontrado { get; init; }
    public string? Xml { get; init; }
    public bool Error { get; init; }
    public string? MensajeError { get; init; }

    public static BuscarXmlResponse ConXml(string xml) => new()
    {
        Encontrado = true,
        Xml = xml
    };

    public static BuscarXmlResponse SinResultados() => new()
    {
        Encontrado = false
    };

    public static BuscarXmlResponse ConError(string mensajeError) => new()
    {
        Error = true,
        MensajeError = mensajeError
    };
}
