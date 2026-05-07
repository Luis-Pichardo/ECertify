using eCertify.Models.ResumenesModel;
using eCertify.Models;
using System.Xml.Linq;

namespace eCertify.Interfaces
{
    public interface IResumenesXmlService
    {
        Task<string> GenerarXmlDesdeModeloAsync(ResumenesModel resumen);
        ResumenesModel MapearResumenDesdeFactura(FacturasModels factura);
        ResumenesModel MapearResumenDesdeDiccionario(Dictionary<string, string> rowData);
        XElement CrearXmlDesdeObjeto(object obj);
    }
}
