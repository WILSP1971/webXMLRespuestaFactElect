namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Opciones de configuracion (no secretas) para la capa de acceso a datos.
/// Se leen desde la seccion "FactElectronicaDb" de appsettings/entorno (NF-5).
/// </summary>
public sealed class FactElectronicaDbOptions
{
    public const string SeccionConfiguracion = "FactElectronicaDb";

    public int ComandoTimeoutSegundos { get; set; } = 30;

    public int ConexionTimeoutSegundos { get; set; } = 15;
}
