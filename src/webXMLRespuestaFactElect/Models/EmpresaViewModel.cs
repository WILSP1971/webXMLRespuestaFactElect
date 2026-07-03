namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Representa una empresa para el dropdown F-1 (Empresa).
/// SUPUESTO S-1: se asume que GetEmpresas devuelve al menos un codigo/id y un nombre;
/// el mapeo tolerante a nombres de columna se resuelve en FactElectronicaRepository.
/// Debe confirmarse con el Lead el nombre exacto de las columnas de salida.
/// </summary>
public sealed class EmpresaViewModel
{
    /// <summary>Codigo/Id de la empresa (valor del &lt;option&gt;, usado como @Empresa en Get_LogWebService).</summary>
    public string Codigo { get; init; } = string.Empty;

    /// <summary>Nombre de la empresa (texto del &lt;option&gt; y valor mostrado en F-2).</summary>
    public string Nombre { get; init; } = string.Empty;
}
