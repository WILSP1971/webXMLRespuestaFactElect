using System.Data;
using webXMLRespuestaFactElect.Models;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Mapeo tolerante (case-insensitive, con varios nombres de columna candidatos) de las
/// filas devueltas por `GetEmpresas` y `Get_TipoDocumentosFactElect` hacia los
/// ViewModels de los dropdowns F-1 y F-3. Aislado de la conexion real para poder
/// probarse con dobles/mocks (CHECKPOINT C7).
/// </summary>
public static class CatalogosQuery
{
    public const string NombreStoredProcedureEmpresas = "Getempresas";
    public const string NombreStoredProcedureTipoDocumentos = "Get_TipoDocumentosFactElect";

    // S-1 CONFIRMADO por el Lead: Getempresas devuelve, entre otras, las columnas
    // "empresa" (codigo) y "NombreEmpresa". Se mantienen las variantes previas como
    // tolerancia adicional.
    private static readonly string[] PosiblesColumnasCodigoEmpresa =
    {
        "Empresa", "Codigo", "CodEmpresa", "CodigoEmpresa", "IdEmpresa", "Id"
    };

    private static readonly string[] PosiblesColumnasNombreEmpresa =
    {
        "NombreEmpresa", "Nombre", "RazonSocial", "Descripcion", "Empresa_Nombre"
    };

    // S-2 CONFIRMADO por el Lead: Get_TipoDocumentosFactElect devuelve las columnas
    // Empresa, CodDocumento, NombreDocumento, TipoDocumento. Se mantienen las
    // variantes previas como tolerancia adicional.
    private static readonly string[] PosiblesColumnasCodigoTipoDoc =
    {
        "CodDocumento", "Codigo", "CodTipoDoc", "TipoDoc", "IdTipoDoc", "Id"
    };

    private static readonly string[] PosiblesColumnasDescripcionTipoDoc =
    {
        "NombreDocumento", "Descripcion", "Nombre", "NombreTipoDoc", "TipoDocumento"
    };

    public static EmpresaViewModel MapearEmpresa(IDataRecord fila)
    {
        ArgumentNullException.ThrowIfNull(fila);

        return new EmpresaViewModel
        {
            Codigo = ObtenerTexto(fila, PosiblesColumnasCodigoEmpresa),
            Nombre = ObtenerTexto(fila, PosiblesColumnasNombreEmpresa)
        };
    }

    public static TipoDocumentoViewModel MapearTipoDocumento(IDataRecord fila)
    {
        ArgumentNullException.ThrowIfNull(fila);

        return new TipoDocumentoViewModel
        {
            Codigo = ObtenerTexto(fila, PosiblesColumnasCodigoTipoDoc),
            Descripcion = ObtenerTexto(fila, PosiblesColumnasDescripcionTipoDoc)
        };
    }

    private static string ObtenerTexto(IDataRecord fila, IReadOnlyList<string> posiblesNombres)
    {
        var indice = GetLogWebServiceQuery.BuscarIndiceColumna(fila, posiblesNombres);
        if (indice is null || fila.IsDBNull(indice.Value))
        {
            return string.Empty;
        }

        return fila.GetValue(indice.Value)?.ToString() ?? string.Empty;
    }
}
