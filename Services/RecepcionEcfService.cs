using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using eCertify.Interfaces;
using System.Xml;

namespace eCertify.Services
{
    public class RecepcionEcfService : IRecepcionEcfService
    {
        private readonly ILogger<RecepcionEcfService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ISemillaService _semillaService;
        private readonly SogeDbContext _context;
        private const string LogPrefix = "[RecepcionECFService]";

        public RecepcionEcfService(ILogger<RecepcionEcfService> logger, IWebHostEnvironment env, ISemillaService semillaService, SogeDbContext context)
        {
            _logger = logger;
            _env = env;
            _logger.LogInformation($"{LogPrefix} Servicio inicializado");
            _semillaService = semillaService;
            _context = context;
        }

        public async Task<string> ProcesarEcfAsync(IFormFile archivoXml)
        {
            const string methodName = "ProcesarEcfAsync";
            _logger.LogInformation($"{LogPrefix} {methodName} - Inicio de procesamiento");

            try
            {
                // Validación inicial del archivo
                var validacion = await ValidarArchivoEcfAsync(archivoXml);
                if (!validacion.isValid)
                {
                    _logger.LogWarning($"{LogPrefix} {methodName} - Validación fallida: {validacion.errorMessage}");
                    throw new ArgumentException(validacion.errorMessage);
                }

                // Leer y parsear el XML
                string xmlContent;
                using (var reader = new StreamReader(archivoXml.OpenReadStream()))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                _logger.LogDebug($"{LogPrefix} {methodName} - XML recibido: {xmlContent}");

                var xmlEcf = new XmlDocument();
                xmlEcf.LoadXml(xmlContent);

                // Extraer datos del ECF
                var datosEcf = ExtraerDatosEcf(xmlEcf);

                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == datosEcf.rncComprador);
                if (empresa == null)
                {
                    _logger.LogError($"{LogPrefix} {methodName} - Empresa con RNC {datosEcf.rncComprador} no encontrada.");
                    throw new Exception($"Empresa con RNC {datosEcf.rncComprador} no encontrada");
                }

                // Generar ARECF
                var arecf = GenerarARECF(datosEcf);

                // Firmar el documento
                var xmlFirmado = _semillaService.FirmarXml(arecf, empresa, false);

                _logger.LogInformation($"{LogPrefix} {methodName} - Procesamiento completado");
                return xmlFirmado.OuterXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{LogPrefix} {methodName} - Error al procesar ECF");
                throw;
            }
        }

        public async Task<(bool isValid, string errorMessage)> ValidarArchivoEcfAsync(IFormFile archivoXml)
        {
            if (archivoXml == null || archivoXml.Length == 0)
                return (false, "No se recibió ningún archivo XML");

            if (Path.GetExtension(archivoXml.FileName).ToLower() != ".xml")
                return (false, "El archivo debe ser un XML");

            return (true, string.Empty);
        }

        private (string rncEmisor, string rncComprador, string encf) ExtraerDatosEcf(XmlDocument xmlEcf)
        {
            var rncEmisor = xmlEcf.SelectSingleNode("//Emisor/RNCEmisor")?.InnerText;
            var rncComprador = xmlEcf.SelectSingleNode("//Comprador/RNCComprador")?.InnerText;
            var encf = xmlEcf.SelectSingleNode("//IdDoc/eNCF")?.InnerText;

            if (string.IsNullOrEmpty(rncEmisor) || string.IsNullOrEmpty(encf))
                throw new XmlException("El documento ECF no contiene los campos requeridos (RNCEmisor, eNCF)");

            return (rncEmisor, rncComprador, encf);
        }

        private XmlDocument GenerarARECF((string rncEmisor, string rncComprador, string encf) datos)
        {
            var xmlDoc = new XmlDocument();
            var xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            var arecfElement = xmlDoc.CreateElement("ARECF");
            arecfElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            arecfElement.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
            xmlDoc.AppendChild(arecfElement);

            var detalleElement = xmlDoc.CreateElement("DetalleAcusedeRecibo");
            arecfElement.AppendChild(detalleElement);

            AddXmlElement(xmlDoc, detalleElement, "Version", "1.0");
            AddXmlElement(xmlDoc, detalleElement, "RNCEmisor", datos.rncEmisor);
            AddXmlElement(xmlDoc, detalleElement, "RNCComprador", datos.rncComprador ?? "000000000");
            AddXmlElement(xmlDoc, detalleElement, "eNCF", datos.encf);
            AddXmlElement(xmlDoc, detalleElement, "Estado", "0");
            AddXmlElement(xmlDoc, detalleElement, "FechaHoraAcuseRecibo", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

            return xmlDoc;
        }

        //private XmlDocument FirmarDocumento(XmlDocument xmlDoc)
        //{
        //    string certificatePath = Path.Combine(_env.ContentRootPath, Utils.Constants.CertificatePath);
        //    return Utils.Utils.FirmarXml(xmlDoc, certificatePath, Utils.Constants.certificatePassword, addDate: false);
        //}

        private void AddXmlElement(XmlDocument xmlDoc, XmlElement parent, string name, string value)
        {
            var element = xmlDoc.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
        }
    }
}
