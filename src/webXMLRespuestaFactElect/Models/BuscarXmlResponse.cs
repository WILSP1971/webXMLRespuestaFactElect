namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Respuesta JSON de la accion Buscar (F-6) consumida por fetch/AJAX desde la vista.
/// `Registros` vacio es "sin resultados" (AC-6); `Error=true` es un fallo controlado
/// (AC-C3/NF-2). El grid de la vista se llena directamente con `Registros`.
/// </summary>
public sealed class BuscarXmlResponse
{
    public bool Error { get; init; }
    public string? MensajeError { get; init; }
    public IReadOnlyList<LogWebServiceViewModel> Registros { get; init; } = Array.Empty<LogWebServiceViewModel>();

    public static BuscarXmlResponse ConRegistros(IReadOnlyList<LogWebServiceViewModel> registros) => new()
    {
        Registros = registros
    };

    public static BuscarXmlResponse ConError(string mensajeError) => new()
    {
        Error = true,
        MensajeError = mensajeError
    };
}
