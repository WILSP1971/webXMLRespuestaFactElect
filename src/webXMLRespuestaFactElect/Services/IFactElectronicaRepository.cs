using webXMLRespuestaFactElect.Models;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Contrato de acceso a datos de solo lectura hacia FactElectronicaDB, exclusivamente
/// mediante Stored Procedures parametrizados (CHECKPOINT C5). No expone ni permite
/// ejecutar SQL de negocio inline desde controladores o vistas.
/// </summary>
public interface IFactElectronicaRepository
{
    /// <summary>
    /// Ejecuta el SP `GetEmpresas` (sin parametros) para poblar el dropdown F-1.
    /// </summary>
    Task<OperationResult<IReadOnlyList<EmpresaViewModel>>> ObtenerEmpresasAsync(CancellationToken ct = default);

    /// <summary>
    /// Ejecuta el SP `Get_TipoDocumentosFactElect` (SUPUESTO S-2: sin parametros) para
    /// poblar el dropdown F-3.
    /// </summary>
    Task<OperationResult<IReadOnlyList<TipoDocumentoViewModel>>> ObtenerTipoDocumentosAsync(CancellationToken ct = default);

    /// <summary>
    /// Ejecuta el SP `Get_LogWebService @Empresa, @TipoDoc, @Prefijo, @NoDocumento` y
    /// devuelve TODAS las filas del historial (FechaHoraLog, MetodoWs, RespuestaXML)
    /// para el grid de la vista (F-6). Lista vacia = "sin resultados" (AC-6).
    /// Ejemplo real: EXEC Get_LogWebService '07','FA','33',185138
    /// </summary>
    Task<OperationResult<IReadOnlyList<LogWebServiceViewModel>>> ObtenerHistorialLogAsync(
        string empresa,
        string tipoDoc,
        string prefijo,
        long noDocumento,
        CancellationToken ct = default);
}
