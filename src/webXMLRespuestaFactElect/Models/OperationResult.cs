namespace webXMLRespuestaFactElect.Models;

/// <summary>
/// Envoltorio de resultado para operaciones de la capa de datos que pueden fallar
/// por causas de infraestructura (conexion/timeout/SP). Permite a los controladores
/// devolver mensajes controlados sin propagar excepciones ni detalles tecnicos al
/// cliente (NF-2 / AC-C3).
/// </summary>
/// <typeparam name="T">Tipo del valor cuando la operacion es exitosa.</typeparam>
public sealed class OperationResult<T>
{
    public bool Exitoso { get; private init; }
    public T? Valor { get; private init; }

    /// <summary>Mensaje de error controlado, seguro para mostrar al usuario final (sin stack trace).</summary>
    public string? MensajeError { get; private init; }

    public static OperationResult<T> Ok(T valor) => new()
    {
        Exitoso = true,
        Valor = valor
    };

    public static OperationResult<T> Fallo(string mensajeError) => new()
    {
        Exitoso = false,
        MensajeError = mensajeError
    };
}
