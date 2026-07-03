namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Representa un tipo de documento para el dropdown F-3 (Tipo de documento).
/// SUPUESTO S-2: se asume que Get_TipoDocumentosFactElect no recibe parametros y
/// devuelve un codigo + una descripcion; debe confirmarse con el Lead la firma real
/// (podria filtrar por empresa) y los nombres exactos de columna.
/// </summary>
public sealed class TipoDocumentoViewModel
{
    /// <summary>Codigo del tipo de documento (valor del &lt;option&gt;, usado como @TipoDoc en Get_LogWebService).</summary>
    public string Codigo { get; init; } = string.Empty;

    /// <summary>Descripcion del tipo de documento (texto del &lt;option&gt;).</summary>
    public string Descripcion { get; init; } = string.Empty;
}
