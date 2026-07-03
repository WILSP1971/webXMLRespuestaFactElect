namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Representa un tipo de documento para el dropdown F-3 (Tipo de documento).
/// S-2 CONFIRMADO: Get_TipoDocumentosFactElect recibe @Empresa (obligatorio) y
/// devuelve, entre otras, las columnas CodDocumento y NombreDocumento, mapeadas aqui
/// a Codigo/Descripcion. El dropdown se muestra como "Codigo - Descripcion"
/// (ej. "FA - FACTURA ESCULAPIO").
/// </summary>
public sealed class TipoDocumentoViewModel
{
    /// <summary>Codigo del tipo de documento (valor del &lt;option&gt;, usado como @TipoDoc en Get_LogWebService).</summary>
    public string Codigo { get; init; } = string.Empty;

    /// <summary>Descripcion del tipo de documento (texto del &lt;option&gt;).</summary>
    public string Descripcion { get; init; } = string.Empty;
}
