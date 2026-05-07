using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using eCertify.Interfaces;
using eCertify.Utils;
using System.Xml;

namespace eCertify.Controllers
{
    [Route("fe/recepcion/api/ecf")]
    [ApiController]
    public class RecepcionEcfController : ControllerBase
    {
        private readonly ILogger<RecepcionEcfController> _logger;
        private readonly IRecepcionEcfService _recepcionEcfService;
        private readonly IFileStorageManager _fileStorageManager;
        private const string LogPrefix = "[RecepcionECFController]";

        public RecepcionEcfController(
            ILogger<RecepcionEcfController> logger,
            IRecepcionEcfService recepcionEcfService,
            IFileStorageManager fileStorageManager)
        {
            _logger = logger;
            _recepcionEcfService = recepcionEcfService;
            _logger.LogInformation($"{LogPrefix} Controlador inicializado");
            _fileStorageManager = fileStorageManager;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ARECF(IFormFile xml)
        {
            const string methodName = "ARECF";
            _logger.LogInformation($"{LogPrefix} {methodName} - Solicitud recibida");

            try
            {
                var xmlResultado = await _recepcionEcfService.ProcesarEcfAsync(xml);
                _logger.LogInformation($"{LogPrefix} {methodName} - Procesamiento exitoso");

                // Convertir a XmlDocument para extraer el RNC
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlResultado);

                var rncComprador = ExtraerRncComprador(xmlDoc);

                var fechaActual = DateTime.Now.ToString("yyyyMMddHHmmss");
                var nombreArchivo = $"{rncComprador}_{fechaActual}.xml";

                // Guardar el acuse
                await _fileStorageManager.SaveARECFAsync(
                    rncComprador,
                    xmlResultado,
                    Path.Combine("ARECF", rncComprador, nombreArchivo),
                    FileStorageManager.StorageType.Certificacion
                );

                return new ContentResult
                {
                    Content = xmlResultado,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"{LogPrefix} {methodName} - Error de validación: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, $"{LogPrefix} {methodName} - Error en XML");
                return BadRequest($"Error al procesar el XML: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{LogPrefix} {methodName} - Error inesperado");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        private string ExtraerRncComprador(XmlDocument xml)
        {
            var nodoRnc = xml.SelectSingleNode("//DetalleAcusedeRecibo/RNCComprador");
            if (nodoRnc == null || string.IsNullOrWhiteSpace(nodoRnc.InnerText))
                throw new Exception("No se pudo extraer el RNC del acuse.");

            return nodoRnc.InnerText.Trim();
        }

    }
}
