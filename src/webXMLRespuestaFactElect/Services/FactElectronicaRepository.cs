using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using webXMLRespuestaFactElect.Models;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Implementacion de solo lectura de <see cref="IFactElectronicaRepository"/> sobre
/// SQL Server, usando Microsoft.Data.SqlClient (ADO.NET) y ejecutando exclusivamente
/// Stored Procedures parametrizados (CHECKPOINT C5). No contiene SQL de negocio
/// inline: solo `CommandType.StoredProcedure` + parametros.
///
/// La cadena de conexion se obtiene de configuracion (clave
/// "ConnectionStrings:CadenaConexionDB"), nunca hardcodeada (CHECKPOINT C3): en
/// desarrollo via User Secrets, en IIS via variables de entorno / secret manager.
/// </summary>
public sealed class FactElectronicaRepository : IFactElectronicaRepository
{
    private readonly string _cadenaConexion;
    private readonly FactElectronicaDbOptions _opciones;
    private readonly ILogger<FactElectronicaRepository> _logger;

    private const string MensajeErrorGenerico =
        "No fue posible completar la operacion contra la base de datos. Intente nuevamente en unos minutos.";

    public FactElectronicaRepository(
        IConfiguration configuracion,
        IOptions<FactElectronicaDbOptions> opciones,
        ILogger<FactElectronicaRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(opciones);

        _cadenaConexion = configuracion.GetConnectionString("CadenaConexionDB") ?? string.Empty;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<OperationResult<IReadOnlyList<EmpresaViewModel>>> ObtenerEmpresasAsync(CancellationToken ct = default)
    {
        try
        {
            var resultado = new List<EmpresaViewModel>();

            await using var conexion = await AbrirConexionAsync(ct);
            await using var comando = CrearComando(conexion, CatalogosQuery.NombreStoredProcedureEmpresas);

            await using var lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Add(CatalogosQuery.MapearEmpresa(lector));
            }

            return OperationResult<IReadOnlyList<EmpresaViewModel>>.Ok(resultado);
        }
        catch (Exception ex) when (EsErrorDeInfraestructura(ex))
        {
            RegistrarErrorInfraestructura(ex, CatalogosQuery.NombreStoredProcedureEmpresas);
            return OperationResult<IReadOnlyList<EmpresaViewModel>>.Fallo(MensajeErrorGenerico);
        }
    }

    public async Task<OperationResult<IReadOnlyList<TipoDocumentoViewModel>>> ObtenerTipoDocumentosAsync(CancellationToken ct = default)
    {
        try
        {
            var resultado = new List<TipoDocumentoViewModel>();

            await using var conexion = await AbrirConexionAsync(ct);
            await using var comando = CrearComando(conexion, CatalogosQuery.NombreStoredProcedureTipoDocumentos);

            await using var lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Add(CatalogosQuery.MapearTipoDocumento(lector));
            }

            return OperationResult<IReadOnlyList<TipoDocumentoViewModel>>.Ok(resultado);
        }
        catch (Exception ex) when (EsErrorDeInfraestructura(ex))
        {
            RegistrarErrorInfraestructura(ex, CatalogosQuery.NombreStoredProcedureTipoDocumentos);
            return OperationResult<IReadOnlyList<TipoDocumentoViewModel>>.Fallo(MensajeErrorGenerico);
        }
    }

    public async Task<OperationResult<IReadOnlyList<LogWebServiceViewModel>>> ObtenerHistorialLogAsync(
        string empresa,
        string tipoDoc,
        string prefijo,
        long noDocumento,
        CancellationToken ct = default)
    {
        try
        {
            var resultado = new List<LogWebServiceViewModel>();

            await using var conexion = await AbrirConexionAsync(ct);
            await using var comando = CrearComando(conexion, GetLogWebServiceQuery.NombreStoredProcedure);
            comando.Parameters.AddRange(GetLogWebServiceQuery.ConstruirParametros(empresa, tipoDoc, prefijo, noDocumento));

            await using var lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Add(GetLogWebServiceQuery.MapearFila(lector));
            }

            // Lista vacia: "sin resultados" (AC-6), no es un error.
            return OperationResult<IReadOnlyList<LogWebServiceViewModel>>.Ok(resultado);
        }
        catch (Exception ex) when (EsErrorDeInfraestructura(ex))
        {
            RegistrarErrorInfraestructura(ex, GetLogWebServiceQuery.NombreStoredProcedure);
            return OperationResult<IReadOnlyList<LogWebServiceViewModel>>.Fallo(MensajeErrorGenerico);
        }
    }

    private async Task<SqlConnection> AbrirConexionAsync(CancellationToken ct)
    {
        var builder = new SqlConnectionStringBuilder(_cadenaConexion)
        {
            ConnectTimeout = _opciones.ConexionTimeoutSegundos
        };

        var conexion = new SqlConnection(builder.ConnectionString);
        await conexion.OpenAsync(ct);
        return conexion;
    }

    private SqlCommand CrearComando(SqlConnection conexion, string nombreStoredProcedure)
    {
        return new SqlCommand(nombreStoredProcedure, conexion)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _opciones.ComandoTimeoutSegundos
        };
    }

    /// <summary>
    /// Determina si la excepcion corresponde a una falla de infraestructura o de
    /// configuracion (conexion, timeout, cadena de conexion ausente/mal formada,
    /// ejecucion del SP) que debe traducirse a un mensaje controlado (NF-2 / AC-C3)
    /// en vez de propagarse como stack trace/HTML al cliente que espera JSON.
    /// Incluye <see cref="ArgumentException"/> y <see cref="FormatException"/> porque
    /// <see cref="SqlConnectionStringBuilder"/>/<see cref="SqlConnection"/> las lanzan
    /// cuando la cadena de conexion esta ausente o mal formada (FIX 2, ver revision
    /// BLACK PANTHER/WOLVERINE).
    /// </summary>
    private static bool EsErrorDeInfraestructura(Exception ex) =>
        ex is SqlException or InvalidOperationException or TimeoutException or ArgumentException or FormatException;

    /// <summary>
    /// Registra el error de infraestructura con un mensaje de log distinto segun la
    /// causa, para facilitar el diagnostico: cadena de conexion mal formada/ausente
    /// (error de configuracion, requiere revisar appsettings/User Secrets/variables de
    /// entorno) vs. fallo real de conectividad/ejecucion contra SQL Server.
    /// </summary>
    private void RegistrarErrorInfraestructura(Exception ex, string nombreStoredProcedure)
    {
        if (ex is ArgumentException or FormatException)
        {
            _logger.LogError(
                ex,
                "Error de configuracion de la cadena de conexion (ConnectionStrings:CadenaConexionDB) al ejecutar {StoredProcedure}",
                nombreStoredProcedure);
        }
        else
        {
            _logger.LogError(
                ex,
                "Error de conectividad/ejecucion contra la base de datos al ejecutar {StoredProcedure}",
                nombreStoredProcedure);
        }
    }
}
