using webXMLRespuestaFactElect.Models;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Contrato de acceso a datos de solo lectura hacia FactElectronicaDB, exclusivamente
/// mediante Stored Procedures parametrizados (CHECKPOINT C5).
/// </summary>
public interface IFactElectronicaRepository
{
    /// <summary>
    /// Ejecuta el SP `GetEmpresas` (sin parametros) para poblar el dropdown F-1.
    /// </summary>
    Task<OperationResult<IReadOnlyList<EmpresaViewModel>>> ObtenerEmpresasAsync(CancellationToken ct = default);

    /// <summary>
    /// Ejecuta el SP `Get_TipoDocumentosFactElect` para poblar el dropdown F-3.
    /// Si `empresa` viene con valor, se pasa como parametro al SP para filtrar.
    /// Si el SP todavia no acepta ese parametro, el repo lo ignora (compatibilidad
    /// hacia atras: la lista simplemente no se filtra).
    /// </summary>
    Task<OperationResult<IReadOnlyList<TipoDocumentoViewModel>>> ObtenerTipoDocumentosAsync(
        string? empresa = null,
        CancellationToken ct = default);

    /// <summary>
    /// Ejecuta el SP `Get_LogWebService @Empresa, @TipoDoc, @Prefijo, @NoDocumento` y
    /// devuelve TODAS las filas del historial para el grid.
    /// </summary>
    Task<OperationResult<IReadOnlyList<LogWebServiceViewModel>>> ObtenerHistorialLogAsync(
        string empresa,
        string tipoDoc,
        string prefijo,
        long noDocumento,
        CancellationToken ct = default);
}
