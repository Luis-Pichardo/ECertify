// Controlador para recibir el XML, extraer los datos relevantes y generar el RFCE
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using eCertify.Interfaces;
using System.Xml;
using eCertify.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    //Posibilidad de eliminarse ya que no se usa
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RecepcionFacturasE32 : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ISemillaService _semillaService;
        private readonly IFileStorageManager _fileStorageManager;
        private readonly eCertify.Data.SogeDbContext _context;

        public RecepcionFacturasE32(IWebHostEnvironment env, ISemillaService semillaService, IFileStorageManager fileStorageManager, eCertify.Data.SogeDbContext context)
        {
            _env = env;
            _semillaService = semillaService;
            _fileStorageManager = fileStorageManager;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> GenerarRFCEConFirma([FromForm] IFormFile archivoXml)
        {
            if (archivoXml == null || archivoXml.Length == 0)
                return BadRequest("El archivo XML es requerido.");

            string contenidoXml;
            using (var reader = new StreamReader(archivoXml.OpenReadStream()))
                contenidoXml = await reader.ReadToEndAsync();

            var doc = XDocument.Parse(contenidoXml);

            // Validar que exista la firma en el XML antes de continuar
            var signatureValue = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "SignatureValue")?.Value;
            if (string.IsNullOrEmpty(signatureValue) || signatureValue.Length < 6)
                return BadRequest("El XML no contiene una firma válida con al menos 6 caracteres en SignatureValue.");

            var rfce = new XElement("RFCE");
            var encabezado = doc.Root?.Element("Encabezado");
            if (encabezado == null) return BadRequest("No se encontró el nodo Encabezado.");

            var rfceEncabezado = new XElement("Encabezado");

            void CopyElement(string name, XElement source, XElement target)
            {
                var elem = source.Element(name);
                if (elem != null && !string.IsNullOrWhiteSpace(elem.Value))
                    target.Add(new XElement(name, elem.Value));
            }

            CopyElement("Version", encabezado, rfceEncabezado);

            var idDoc = encabezado.Element("IdDoc");
            if (idDoc != null)
            {
                var rfceIdDoc = new XElement("IdDoc");
                foreach (var name in new[] { "TipoeCF", "eNCF", "TipoIngresos", "TipoPago" })
                    CopyElement(name, idDoc, rfceIdDoc);

                var tablaFormasPago = idDoc.Element("TablaFormasPago");
                if (tablaFormasPago != null)
                {
                    var rfceTablaFormasPago = new XElement("TablaFormasPago");
                    foreach (var forma in tablaFormasPago.Elements("FormaDePago"))
                    {
                        var nueva = new XElement("FormaDePago");
                        CopyElement("FormaPago", forma, nueva);
                        CopyElement("MontoPago", forma, nueva);
                        rfceTablaFormasPago.Add(nueva);
                    }
                    rfceIdDoc.Add(rfceTablaFormasPago);
                }
                rfceEncabezado.Add(rfceIdDoc);
            }

            var emisor = encabezado.Element("Emisor");
            var rnc = emisor?.Element("RNCEmisor")?.Value?.Trim() ?? "SINRNC";
            if (string.IsNullOrWhiteSpace(rnc)) return BadRequest("No se pudo obtener el RNC del emisor desde el XML.");

            var rfceEmisor = new XElement("Emisor");
            foreach (var name in new[] { "RNCEmisor", "RazonSocialEmisor", "FechaEmision" })
                CopyElement(name, emisor, rfceEmisor);
            rfceEncabezado.Add(rfceEmisor);

            var comprador = encabezado.Element("Comprador");
            if (comprador != null)
            {
                var rfceComprador = new XElement("Comprador");
                foreach (var name in new[] { "RNCComprador", "IdentificadorExtranjero", "RazonSocialComprador" })
                    CopyElement(name, comprador, rfceComprador);
                rfceEncabezado.Add(rfceComprador);
            }

            var totales = encabezado.Element("Totales");
            if (totales != null)
            {
                var rfceTotales = new XElement("Totales");
                foreach (var name in new[]
                {
                    "MontoGravadoTotal", "MontoGravadoI1", "MontoGravadoI2", "MontoGravadoI3",
                    "MontoExento", "TotalITBIS", "TotalITBIS1", "TotalITBIS2", "TotalITBIS3",
                    "MontoImpuestoAdicional", "MontoTotal", "MontoNoFacturable", "MontoPeriodo"
                })
                    CopyElement(name, totales, rfceTotales);

                var impuestos = totales.Element("ImpuestosAdicionales");
                if (impuestos != null)
                {
                    var rfceImpuestos = new XElement("ImpuestosAdicionales");
                    foreach (var imp in impuestos.Elements("ImpuestoAdicional"))
                    {
                        var nuevoImp = new XElement("ImpuestoAdicional");
                        foreach (var name in new[]
                        {
                            "TipoImpuesto", "MontoImpuestoSelectivoConsumoEspecifico",
                            "MontoImpuestoSelectivoConsumoAdvalorem", "OtrosImpuestosAdicionales"
                        })
                            CopyElement(name, imp, nuevoImp);
                        rfceImpuestos.Add(nuevoImp);
                    }
                    rfceTotales.Add(rfceImpuestos);
                }
                rfceEncabezado.Add(rfceTotales);
            }

            rfceEncabezado.Add(new XElement("CodigoSeguridadeCF", signatureValue.Substring(0, 6)));
            rfce.Add(rfceEncabezado);

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == rnc);
            if (empresa == null) return NotFound($"No se encontró la empresa con RNC {rnc}.");

            var xmlFinal = new XmlDocument();
            xmlFinal.LoadXml(rfce.ToString());
            XmlDocument xmlFirmado = _semillaService.FirmarXml(xmlFinal, empresa);

            var eNCF = encabezado?.Element("IdDoc")?.Element("eNCF")?.Value ?? "SINENCF";
            var folder = Path.Combine(_env.ContentRootPath, "Storage", "Facturas", "Recepcion", rnc);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string nombreFinal = $"{rnc}{eNCF}.xml";
            //await _fileStorageManager.SaveXmlAsync(rnc, xmlFirmado.OuterXml, nombreFinal);
            var path = Path.Combine(folder, nombreFinal);

            xmlFirmado.Save(path);

            return Ok(new { message = "RFCE firmado y guardado exitosamente", fileName = nombreFinal });
        }
    }
}
