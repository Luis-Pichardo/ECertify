using eCertify.Models;
using System.Xml;

namespace eCertify.Interfaces
{
    public interface IAprobacionComercialService
    {
        //Task<string> EnviarAprobacion(XmlDocument xmlDocument, string fileName, string bearerToken);

        Task<string> GenerarYFirmarXmlAsync(ACECF modelo);

        (bool Exito, string Mensaje) ValidarAprobacionComercial(string xmlString);
    }
}