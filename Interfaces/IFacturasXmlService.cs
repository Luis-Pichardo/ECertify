using eCertify.DTOs;
using eCertify.Models;
using eCertify.Models.ResumenesModel;
using System.Xml;
using System.Xml.Linq;

namespace eCertify.Interfaces
{
    public interface IFacturasXmlService
    {
        Task<string> GenerarXmlDesdeModeloAsync(FacturasModels factura);
        Task<string> GenerarXmlsDesdeExcelAsync(string rutaExcel);

        FacturasModels MapearFacturaDesdeDiccionario(Dictionary<string, string> data);

        XElement CrearXmlDesdeObjeto(object modelo);

        //Nuevas interfaces

        Task ValidarFacturaAsync(FacturasModels factura);
        (ExtraInfoFacturaDTO info, XmlDocument firmadoXml) ExtraerInformacionDesdeXml(XmlDocument xml, Empresa empresa);
        string GenerarUrlQR(FacturasModels factura, ExtraInfoFacturaDTO info);
        Task<(bool exito, string trackId, string mensaje)> EnviarFacturaADGIIAsync(string xmlFirmado, string fileName, string tipoECF, decimal montoTotal, string rncEmisor);
        Task<string> ConsultarEstadoDGIIAsync(string trackId, string rncEmisor);
        Task<bool> GuardarConsultasFacturaAsync(FacturaResponseDto respuesta);


    }
}