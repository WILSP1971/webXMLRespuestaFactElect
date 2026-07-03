namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Representa una empresa para el dropdown F-1 (Empresa).
/// S-1 CONFIRMADO: Getempresas devuelve, entre otras, las columnas "empresa"
/// (codigo) y "NombreEmpresa", mapeadas aqui a Codigo/Nombre. El dropdown se
/// muestra como "Codigo - Nombre" (ej. "07 - Fundacion Campobell").
/// </summary>
public sealed class EmpresaViewModel
{
    /// <summary>Codigo/Id de la empresa (valor del &lt;option&gt;, usado como @Empresa en Get_LogWebService).</summary>
    public string Codigo { get; init; } = string.Empty;

    /// <summary>Nombre de la empresa (texto del &lt;option&gt; y valor mostrado en F-2).</summary>
    public string Nombre { get; init; } = string.Empty;
}
