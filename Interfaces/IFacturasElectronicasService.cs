using System.Xml;
using eCertify.Services;

namespace eCertify.Interfaces
{
    public interface IFacturasElectronicasService
    {
        Task<FacturasElectronicasService.ApiResponse> ConsultarFacturaEnviada(string trackId, string bearerToken);
        Task<string> EnviarFactura(XmlDocument xmlDocument, string fileName, string bearerToken);
    }
}