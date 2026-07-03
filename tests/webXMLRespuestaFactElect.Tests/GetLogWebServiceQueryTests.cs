using System.Data;
using webXMLRespuestaFactElect.Services;
using Xunit;

namespace webXMLRespuestaFactElect.Tests;

/// <summary>
/// Pruebas de CHECKPOINT C7 / AC-C2: verifican el armado de parametros y el mapeo de
/// resultados de Get_LogWebService, con dobles de prueba (sin base de datos real).
/// </summary>
public class GetLogWebServiceQueryTests
{
    [Fact]
    public void ConstruirParametros_ArmaLosCuatroParametrosEnElOrdenYTipoEsperado()
    {
        // Ejemplo real confirmado por el Lead: EXEC Get_LogWebService '07','FA','33',185138
        var parametros = GetLogWebServiceQuery.ConstruirParametros("07", "FA", "33", 185138);

        Assert.Equal(4, parametros.Length);

        Assert.Equal("@Empresa", parametros[0].ParameterName);
        Assert.Equal("07", parametros[0].Value);
        Assert.Equal(SqlDbType.VarChar, parametros[0].SqlDbType);

        Assert.Equal("@TipoDoc", parametros[1].ParameterName);
        Assert.Equal("FA", parametros[1].Value);
        Assert.Equal(SqlDbType.VarChar, parametros[1].SqlDbType);

        Assert.Equal("@Prefijo", parametros[2].ParameterName);
        Assert.Equal("33", parametros[2].Value);
        Assert.Equal(SqlDbType.VarChar, parametros[2].SqlDbType);

        Assert.Equal("@NoDocumento", parametros[3].ParameterName);
        Assert.Equal(185138L, parametros[3].Value);
        Assert.Equal(SqlDbType.BigInt, parametros[3].SqlDbType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstruirParametros_LanzaExcepcion_SiEmpresaEsVaciaOEnBlanco(string empresaInvalida)
    {
        Assert.Throws<ArgumentException>(() =>
            GetLogWebServiceQuery.ConstruirParametros(empresaInvalida, "FA", "33", 185138));
    }

    [Fact]
    public void MapearRespuestaXml_DevuelveElContenido_CuandoLaColumnaExisteYNoEsNula()
    {
        var fila = new FakeDataRecord()
            .ConColumna("RespuestaXML", "<RespuestaDian><Estado>Aceptado</Estado></RespuestaDian>");

        var xml = GetLogWebServiceQuery.MapearRespuestaXml(fila);

        Assert.Equal("<RespuestaDian><Estado>Aceptado</Estado></RespuestaDian>", xml);
    }

    [Fact]
    public void MapearRespuestaXml_EsToleranteAlNombreDeColumnaEnMinusculas()
    {
        // SUPUESTO S-3: el nombre exacto de la columna no esta confirmado; el mapeo
        // debe funcionar sin distinguir mayusculas/minusculas.
        var fila = new FakeDataRecord()
            .ConColumna("respuestaxml", "<A/>");

        var xml = GetLogWebServiceQuery.MapearRespuestaXml(fila);

        Assert.Equal("<A/>", xml);
    }

    [Fact]
    public void MapearRespuestaXml_DevuelveNull_CuandoLaColumnaEsDBNull()
    {
        var fila = new FakeDataRecord()
            .ConColumna("RespuestaXML", DBNull.Value);

        var xml = GetLogWebServiceQuery.MapearRespuestaXml(fila);

        Assert.Null(xml);
    }

    [Fact]
    public void MapearRespuestaXml_DevuelveNull_CuandoNoHayFilaConLaColumnaEsperada()
    {
        // Simula "sin resultados" (AC-6): la fila no trae la columna esperada.
        var fila = new FakeDataRecord()
            .ConColumna("OtraColumna", "valor");

        var xml = GetLogWebServiceQuery.MapearRespuestaXml(fila);

        Assert.Null(xml);
    }

    [Fact]
    public void MapearFila_MapeaFechaHoraLogMetodoWsYRespuestaXml_ParaElGridDeHistorial()
    {
        var fechaEsperada = new DateTime(2026, 6, 30, 14, 5, 0);
        var fila = new FakeDataRecord()
            .ConColumna("FechaHoraLog", fechaEsperada)
            .ConColumna("MetodoWs", "EnvioFactura")
            .ConColumna("RespuestaXML", "<RespuestaDian><Estado>Aceptado</Estado></RespuestaDian>");

        var registro = GetLogWebServiceQuery.MapearFila(fila);

        Assert.Equal(fechaEsperada, registro.FechaHoraLog);
        Assert.Equal("EnvioFactura", registro.MetodoWs);
        Assert.Equal("<RespuestaDian><Estado>Aceptado</Estado></RespuestaDian>", registro.RespuestaXml);
    }

    [Fact]
    public void MapearFila_DevuelveValoresPorDefecto_CuandoLasColumnasNoExistenOSonNulas()
    {
        var fila = new FakeDataRecord()
            .ConColumna("FechaHoraLog", DBNull.Value)
            .ConColumna("MetodoWs", DBNull.Value);

        var registro = GetLogWebServiceQuery.MapearFila(fila);

        Assert.Null(registro.FechaHoraLog);
        Assert.Equal(string.Empty, registro.MetodoWs);
        Assert.Equal(string.Empty, registro.RespuestaXml);
    }
}
