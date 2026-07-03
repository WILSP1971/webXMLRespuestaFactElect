using webXMLRespuestaFactElect.Services;
using Xunit;

namespace webXMLRespuestaFactElect.Tests;

public class XmlFormatterTests
{
    [Fact]
    public void Formatear_IndentaUnXmlDeUnaSolaLinea()
    {
        var xmlCrudo = "<RespuestaDian><Estado>Aceptado</Estado></RespuestaDian>";

        var xmlFormateado = XmlFormatter.Formatear(xmlCrudo);

        Assert.Contains("\n", xmlFormateado);
        Assert.Contains("<Estado>Aceptado</Estado>", xmlFormateado);
    }

    [Fact]
    public void Formatear_DevuelveElTextoOriginal_CuandoNoEsXmlValido()
    {
        var textoNoXml = "no es xml valido <<<";

        var resultado = XmlFormatter.Formatear(textoNoXml);

        Assert.Equal(textoNoXml, resultado);
    }
}
