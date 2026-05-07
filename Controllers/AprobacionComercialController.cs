/*********************************************************************
 *                        DESARROLLADOR ENCARGADO:                    *
 *                             Luís Pichardo                         *
 *                                                                   *
 * Código desarrollado y mantenido por Luís Pichardo, quien es el   *
 * responsable principal de esta implementación.                     *
 *********************************************************************/

using Microsoft.AspNetCore.Mvc;
using System.Xml;
using eCertify.Interfaces;
using eCertify.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    
    [ApiController]
    public class AprobacionComercialController : ControllerBase
    {

        private readonly IAprobacionComercialService _service;
        private readonly ILogger<AprobacionComercialController> _logger;
        private readonly IFileStorageManager _fileStorageManager;

        public AprobacionComercialController(IAprobacionComercialService service, ILogger<AprobacionComercialController> logger, IFileStorageManager fileStorageManager)
        {
            _service = service;
            _logger = logger;
            _fileStorageManager = fileStorageManager;
        }

        /// <summary>
        /// Endpoint que genera y firma un XML de aprobación comercial y lo envía a la DGII.
        /// </summary>
        /// <param name="modelo">Modelo ACECF con los datos de la aprobación comercial.</param>
        /// <returns>Respuesta JSON con el resultado del proceso o un error en caso de fallo.</returns>
        [HttpPost("AprobacionComercial/GenerarXmlACECF")]
        public async Task<IActionResult> GenerarXmlAprobacion([FromBody] ACECF modelo)
        {
            try
            {
                _logger.LogInformation("Solicitud recibida para generar XML de aprobación comercial");

                var respuestaDgii = await _service.GenerarYFirmarXmlAsync(modelo);

                // Parsear la respuesta de DGII para extraer los datos importantes
                var respuestaJson = JsonSerializer.Deserialize<Dictionary<string, object>>(respuestaDgii);

                return Ok(new
                {
                    success = true,
                    estado = respuestaJson["codigo"].ToString() == "01" ? 1 : 2, // 1=Aceptada, 2=Rechazada
                    mensaje = respuestaJson["estado"].ToString(),
                    detalles = respuestaJson["mensaje"] as List<string> ?? new List<string>(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar XML de aprobación comercial");
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Error interno al generar XML",
                    detalle = ex.Message
                });
            }
        }


        /// <summary>
        /// Recibe un archivo XML de aprobación comercial (ACECF), valida su contenido
        /// y lo guarda en el sistema de archivos si la validación es exitosa.
        /// </summary>
        /// <param name="xml">Archivo XML que contiene la información de aprobación comercial.</param>
        /// <returns>
        /// Retorna un resultado HTTP 200 (OK) si el archivo es válido y se guarda correctamente.
        /// Retorna un resultado HTTP 400 (Bad Request) si el archivo está vacío, tiene errores de lectura,
        /// falla la validación o no se encuentra la información necesaria para guardarlo.
        /// </returns>
        //Cuidado con la modificacion de este EndPoint ya que se utiliza para recibir la aprobacion comercial de la DGII, No se le puede modificar la RUTA.
        [HttpPost("/fe/aprobacioncomercial/api/ecf")]
        public async Task<IActionResult> RecepcionAC(IFormFile xml)
        {
            _logger.LogInformation("Inicio de recepción de aprobación comercial.");

            if (xml == null || xml.Length == 0)
            {
                _logger.LogWarning("Archivo XML no proporcionado o está vacío.");
                return BadRequest("Insatisfactorio: archivo XML no proporcionado o vacío.");
            }

            string contenidoXml;
            try
            {
                using var reader = new StreamReader(xml.OpenReadStream());
                contenidoXml = await reader.ReadToEndAsync();
                _logger.LogInformation("Archivo XML leído correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer el archivo XML.");
                return BadRequest("error al leer el archivo XML.");
            }

            try
            {
                var resultado = _service.ValidarAprobacionComercial(contenidoXml);

                if (!resultado.Exito)
                {
                    _logger.LogWarning("Validación fallida: {Mensaje}", resultado.Mensaje);
                    return BadRequest($"Insatisfactorio");
                }

                // Aquí guardamos el XML usando el FileStorageManager:
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(contenidoXml);

                var rncNode = xmlDoc.SelectSingleNode("//ACECF/DetalleAprobacionComercial/RNCComprador");
                var eNCFNode = xmlDoc.SelectSingleNode("//ACECF/DetalleAprobacionComercial/eNCF");

                if (rncNode == null || eNCFNode == null)
                {
                    _logger.LogWarning("No se encontró RNCComprador o eNCF para nombrar el archivo.");
                    return BadRequest("Insatisfactorio");
                }

                string rnc = rncNode.InnerText;
                string eNCF = eNCFNode.InnerText;

                string fileName = $"ACECF_{rnc}_{eNCF}.xml";

                await _fileStorageManager.SaveACECFAsync(contenidoXml, rnc, fileName);

                _logger.LogInformation("Archivo XML guardado correctamente: {fileName}", fileName);

                return Ok("Satisfactorio");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la validación o guardado de aprobación comercial.");
                return BadRequest("Insatisfactorio");
            }
        }

    }
}