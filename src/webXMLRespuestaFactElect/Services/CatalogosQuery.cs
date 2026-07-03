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
    public const string NombreStoredProcedureEmpresas = "GetEmpresas";
    public const string NombreStoredProcedureTipoDocumentos = "Get_TipoDocumentosFactElect";

    // SUPUESTO S-1: nombres de columna de GetEmpresas no confirmados; se intentan varias
    // variantes razonables. Confirmar con el Lead los nombres exactos.
    private static readonly string[] PosiblesColumnasCodigoEmpresa =
    {
        "Codigo", "CodEmpresa", "CodigoEmpresa", "IdEmpresa", "Id", "Empresa"
    };

    private static readonly string[] PosiblesColumnasNombreEmpresa =
    {
        "Nombre", "NombreEmpresa", "RazonSocial", "Descripcion", "Empresa_Nombre"
    };

    // SUPUESTO S-2: nombres de columna de Get_TipoDocumentosFactElect no confirmados.
    private static readonly string[] PosiblesColumnasCodigoTipoDoc =
    {
        "Codigo", "CodTipoDoc", "TipoDoc", "IdTipoDoc", "Id"
    };

    private static readonly string[] PosiblesColumnasDescripcionTipoDoc =
    {
        "Descripcion", "Nombre", "NombreTipoDoc", "TipoDocumento"
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
