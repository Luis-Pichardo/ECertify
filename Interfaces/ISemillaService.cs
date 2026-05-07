using System.Xml;

namespace eCertify.Interfaces
{
    public interface ISemillaService
    {
        Task<string> ObtenerTokenAsync(string rnc);
        Task<string> EnviarXmlFirmado(XmlDocument xmlDoc);
        XmlDocument FirmarXml(XmlDocument xmlDoc, Models.Empresa empresa, bool agregarFechaHora = false);
    }
}