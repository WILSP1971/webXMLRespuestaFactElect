using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using webXMLRespuestaFactElect.Models;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Implementacion de solo lectura de <see cref="IFactElectronicaRepository"/> sobre
/// SQL Server, usando Microsoft.Data.SqlClient (ADO.NET) y ejecutando exclusivamente
/// Stored Procedures parametrizados (CHECKPOINT C5).
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

        _logger.LogInformation("FactElectronicaRepository cargado. ConnectionString length = {Len} chars.", _cadenaConexion.Length);
    }

    public async Task<OperationResult<IReadOnlyList<EmpresaViewModel>>> ObtenerEmpresasAsync(CancellationToken ct = default)
    {
        try
        {
            var resultado = new List<EmpresaViewModel>();
            await using var conexion = await AbrirConexionAsync(ct);
            await using var comando = CrearComando(conexion, CatalogosQuery.NombreStoredProcedureEmpresas);

            _logger.LogInformation("Ejecutando SP: {Sp} (sin parametros)", CatalogosQuery.NombreStoredProcedureEmpresas);

            await using var lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Add(CatalogosQuery.MapearEmpresa(lector));
            }

            _logger.LogInformation("ObtenerEmpresas -> {N} filas", resultado.Count);
            return OperationResult<IReadOnlyList<EmpresaViewModel>>.Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo en ObtenerEmpresasAsync. Excepcion: {Tipo}", ex.GetType().FullName);
            return OperationResult<IReadOnlyList<EmpresaViewModel>>.Fallo(MensajeErrorGenerico);
        }
    }

    /// <summary>
    /// Ejecuta `Get_TipoDocumentosFactElect @Empresa` (parametro OBLIGATORIO segun ALTER
    /// PROCEDURE que confirmaste).
    ///
    /// Columnas que retorna el SP: Empresa, CodDocumento, NombreDocumento, TipoDocumento.
    /// Cuando llamo sin empresa, mando `@Empresa = ''` (string vacio). El SP no falla,
    /// solo devuelve 0 filas. Cuando seleccionan una empresa, se llena el dropdown.
    /// </summary>
    public async Task<OperationResult<IReadOnlyList<TipoDocumentoViewModel>>> ObtenerTipoDocumentosAsync(
        string? codEmpresa = null,
        CancellationToken ct = default)
    {
        try
        {
            var resultado = new List<TipoDocumentoViewModel>();
            await using var conexion = await AbrirConexionAsync(ct);
            await using var comando = CrearComando(conexion, CatalogosQuery.NombreStoredProcedureTipoDocumentos);

            // SIEMPRE pasamos @Empresa porque el SP lo exige. Si codEmpresa es null/empty,
            // pasamos string vacio: el filtro `where Empresa=@Empresa` no encuentra nada
            // y devuelve 0 filas, pero la llamada no falla.
            comando.Parameters.Add(new SqlParameter("@Empresa", SqlDbType.VarChar, 6)
            {
                Value = (object?)codEmpresa ?? string.Empty
            });

            _logger.LogInformation(
                "Ejecutando SP: {Sp} @Empresa='{E}'",
                CatalogosQuery.NombreStoredProcedureTipoDocumentos, codEmpresa ?? "(vacio)");

            await using var lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Add(CatalogosQuery.MapearTipoDocumento(lector));
            }

            _logger.LogInformation("ObtenerTipoDocumentos -> {N} filas (Empresa='{E}')",
                resultado.Count, codEmpresa ?? "(vacio)");
            return OperationResult<IReadOnlyList<TipoDocumentoViewModel>>.Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo en ObtenerTipoDocumentosAsync. Excepcion: {Tipo}", ex.GetType().FullName);
            return OperationResult<IReadOnlyList<TipoDocumentoViewModel>>.Fallo(MensajeErrorGenerico);
        }
    }

    /// <summary>
    /// Ejecuta `Get_LogWebService @Empresa, @CodTipoDocumento, @PrefijoDocumento, @NoDocumentoIni`.
    /// Parametros exactos confirmados por vos contra el ALTER PROCEDURE:
    ///   @Empresa            varchar(6)
    ///   @CodTipoDocumento   varchar(20)
    ///   @PrefijoDocumento   varchar(20)
    ///   @NoDocumentoIni     decimal(20)
    ///
    /// Columnas que retorna: IdLog, FechaHoraLog, MetodoWs, RespuestaXML, Empresa,
    /// CodDocumento, PrefijoSistema, NoFacturaSistema, MensajeSalida, TextoQR,
    /// NoTransaccionFE, InputMetodo.
    /// </summary>
    public async Task<OperationResult<IReadOnlyList<LogWebServiceViewModel>>> ObtenerHistorialLogAsync(
        string codEmpresa, string codTipoDocumento, string prefijoDocumento, long noDocumento, CancellationToken ct = default)
    {
        try
        {
            var resultado = new List<LogWebServiceViewModel>();
            await using var conexion = await AbrirConexionAsync(ct);
            await using var comando = CrearComando(conexion, GetLogWebServiceQuery.NombreStoredProcedure);

            comando.Parameters.Add(new SqlParameter("@Empresa", SqlDbType.VarChar, 6) { Value = codEmpresa });
            comando.Parameters.Add(new SqlParameter("@CodTipoDocumento", SqlDbType.VarChar, 20) { Value = codTipoDocumento });
            comando.Parameters.Add(new SqlParameter("@PrefijoDocumento", SqlDbType.VarChar, 20) { Value = prefijoDocumento });
            comando.Parameters.Add(new SqlParameter("@NoDocumentoIni", SqlDbType.Decimal) { Value = (decimal)noDocumento });

            _logger.LogInformation(
                "Ejecutando SP: {Sp} @Empresa='{E}', @CodTipoDocumento='{T}', @PrefijoDocumento='{P}', @NoDocumentoIni={N}",
                GetLogWebServiceQuery.NombreStoredProcedure, codEmpresa, codTipoDocumento, prefijoDocumento, noDocumento);

            await using var lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Add(GetLogWebServiceQuery.MapearFila(lector));
            }

            _logger.LogInformation("ObtenerHistorialLog -> {N} filas", resultado.Count);
            return OperationResult<IReadOnlyList<LogWebServiceViewModel>>.Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo en ObtenerHistorialLogAsync ({E}/{T}/{P}/{N}). Excepcion: {Tipo}",
                codEmpresa, codTipoDocumento, prefijoDocumento, noDocumento, ex.GetType().FullName);
            return OperationResult<IReadOnlyList<LogWebServiceViewModel>>.Fallo(MensajeErrorGenerico);
        }
    }

    private async Task<SqlConnection> AbrirConexionAsync(CancellationToken ct)
    {
        var csb = new SqlConnectionStringBuilder(_cadenaConexion)
        {
            ConnectTimeout = _opciones.ConexionTimeoutSegundos,
            TrustServerCertificate = true
        };

        var conexion = new SqlConnection(csb.ConnectionString);
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
}
