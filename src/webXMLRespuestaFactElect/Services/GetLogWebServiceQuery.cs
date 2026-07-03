using System.Data;
using Microsoft.Data.SqlClient;
using webXMLRespuestaFactElect.Models;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Encapsula el armado de parametros y el mapeo de resultados del Stored Procedure
/// `Get_LogWebService` (F-6). Aislado en una clase estatica y sin dependencia directa
/// de una conexion abierta para poder probarse con dobles/mocks (CHECKPOINT C7,
/// AC-C2), sin necesidad de una base de datos real.
///
/// Firma confirmada por el Lead via ejemplo de invocacion:
///   EXEC Get_LogWebService '07','FA','33',185138
/// SUPUESTO S-3: la columna de salida se llama exactamente "RespuestaXML"; el mapeo
/// es tolerante (case-insensitive) por si el nombre real difiere ligeramente.
/// </summary>
public static class GetLogWebServiceQuery
{
    public const string NombreStoredProcedure = "Get_LogWebService";

    private static readonly string[] PosiblesNombresColumnaRespuestaXml =
    {
        "RespuestaXML",
        "RespuestaXml",
        "Respuesta_XML",
        "XML"
    };

    private static readonly string[] PosiblesNombresColumnaFechaHoraLog = { "FechaHoraLog" };
    private static readonly string[] PosiblesNombresColumnaMetodoWs = { "MetodoWs" };

    /// <summary>
    /// Construye los 4 parametros de `Get_LogWebService` en el orden y tipo esperados
    /// por el ejemplo real de invocacion (Empresa/TipoDoc/Prefijo como texto,
    /// NoDocumento como entero).
    /// </summary>
    public static SqlParameter[] ConstruirParametros(string empresa, string tipoDoc, string prefijo, long noDocumento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(empresa);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoDoc);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefijo);

        return new[]
        {
            new SqlParameter("@Empresa", SqlDbType.VarChar, 20) { Value = empresa },
            new SqlParameter("@TipoDoc", SqlDbType.VarChar, 20) { Value = tipoDoc },
            new SqlParameter("@Prefijo", SqlDbType.VarChar, 20) { Value = prefijo },
            new SqlParameter("@NoDocumento", SqlDbType.BigInt) { Value = noDocumento }
        };
    }

    /// <summary>
    /// Extrae el contenido de la columna RespuestaXML de una fila de resultado,
    /// buscando el nombre de columna de forma tolerante (ver S-3). Devuelve null si
    /// la columna no existe, es DBNull o esta vacia (para que el llamador lo trate
    /// como "sin resultados", AC-6).
    /// </summary>
    public static string? MapearRespuestaXml(IDataRecord fila)
    {
        ArgumentNullException.ThrowIfNull(fila);

        var indice = BuscarIndiceColumna(fila, PosiblesNombresColumnaRespuestaXml);
        if (indice is null)
        {
            return null;
        }

        if (fila.IsDBNull(indice.Value))
        {
            return null;
        }

        var valor = fila.GetValue(indice.Value)?.ToString();
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    /// <summary>
    /// Mapea una fila completa del historial (FechaHoraLog, MetodoWs, RespuestaXML)
    /// para el grid de resultados de la vista (F-6). Reutiliza <see cref="MapearRespuestaXml"/>
    /// para la columna del XML; el resto usa el mismo mapeo tolerante case-insensitive.
    /// </summary>
    public static LogWebServiceViewModel MapearFila(IDataRecord fila)
    {
        ArgumentNullException.ThrowIfNull(fila);

        return new LogWebServiceViewModel
        {
            FechaHoraLog = ObtenerFecha(fila, PosiblesNombresColumnaFechaHoraLog),
            MetodoWs = ObtenerTexto(fila, PosiblesNombresColumnaMetodoWs),
            RespuestaXml = MapearRespuestaXml(fila) ?? string.Empty
        };
    }

    private static string ObtenerTexto(IDataRecord fila, IReadOnlyList<string> posiblesNombres)
    {
        var indice = BuscarIndiceColumna(fila, posiblesNombres);
        if (indice is null || fila.IsDBNull(indice.Value))
        {
            return string.Empty;
        }

        return fila.GetValue(indice.Value)?.ToString() ?? string.Empty;
    }

    private static DateTime? ObtenerFecha(IDataRecord fila, IReadOnlyList<string> posiblesNombres)
    {
        var indice = BuscarIndiceColumna(fila, posiblesNombres);
        if (indice is null || fila.IsDBNull(indice.Value))
        {
            return null;
        }

        var valor = fila.GetValue(indice.Value);
        if (valor is DateTime fecha)
        {
            return fecha;
        }

        return DateTime.TryParse(valor?.ToString(), out var fechaParseada) ? fechaParseada : null;
    }

    internal static int? BuscarIndiceColumna(IDataRecord fila, IReadOnlyList<string> posiblesNombres)
    {
        for (var i = 0; i < fila.FieldCount; i++)
        {
            var nombreColumna = fila.GetName(i);
            foreach (var candidato in posiblesNombres)
            {
                if (string.Equals(nombreColumna, candidato, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return null;
    }
}
