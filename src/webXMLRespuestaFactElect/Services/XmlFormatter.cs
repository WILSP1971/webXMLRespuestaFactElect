using System.Xml;
using System.Xml.Linq;

namespace webXMLRespuestaFactElect.Services;

/// <summary>
/// Formatea (prettify) el XML crudo devuelto por RespuestaXML para que se muestre
/// legible en el visor F-7 (indentado, un nodo por linea). Si el contenido no es un
/// XML valido, se devuelve el texto original sin modificar (nunca lanza excepcion al
/// llamador) para no convertir un dato inesperado en un error 500.
/// </summary>
public static class XmlFormatter
{
    public static string Formatear(string xmlCrudo)
    {
        if (string.IsNullOrWhiteSpace(xmlCrudo))
        {
            return xmlCrudo;
        }

        try
        {
            var documento = XDocument.Parse(xmlCrudo, LoadOptions.PreserveWhitespace);

            using var lector = new StringWriter();
            using (var escritor = XmlWriter.Create(lector, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = documento.Declaration is null,
                Encoding = System.Text.Encoding.UTF8
            }))
            {
                documento.Save(escritor);
            }

            return lector.ToString();
        }
        catch (Exception ex) when (ex is XmlException or System.Xml.XPath.XPathException)
        {
            // El contenido no es XML valido/parseable: se muestra tal cual llego,
            // cumpliendo igual con "legible" en su forma cruda (no bloqueante AC-7).
            return xmlCrudo;
        }
    }
}
