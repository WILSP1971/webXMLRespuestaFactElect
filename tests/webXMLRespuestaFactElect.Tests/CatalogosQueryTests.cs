using webXMLRespuestaFactElect.Services;
using Xunit;

namespace webXMLRespuestaFactElect.Tests;

public class CatalogosQueryTests
{
    [Fact]
    public void MapearEmpresa_MapeaCodigoYNombre_ConNombresDeColumnaEsperados()
    {
        var fila = new FakeDataRecord()
            .ConColumna("Codigo", "07")
            .ConColumna("Nombre", "Empresa Demo S.A.S.");

        var empresa = CatalogosQuery.MapearEmpresa(fila);

        Assert.Equal("07", empresa.Codigo);
        Assert.Equal("Empresa Demo S.A.S.", empresa.Nombre);
    }

    [Fact]
    public void MapearTipoDocumento_MapeaCodigoYDescripcion()
    {
        var fila = new FakeDataRecord()
            .ConColumna("Codigo", "FA")
            .ConColumna("Descripcion", "Factura Electronica");

        var tipoDocumento = CatalogosQuery.MapearTipoDocumento(fila);

        Assert.Equal("FA", tipoDocumento.Codigo);
        Assert.Equal("Factura Electronica", tipoDocumento.Descripcion);
    }
}
