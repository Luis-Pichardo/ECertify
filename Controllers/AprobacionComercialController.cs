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

namespace eCertify.Controllers
{
    [ApiController]
    public class CommercialApprovalController : ControllerBase
    {
        private readonly ICommercialApprovalService _approvalService;
        private readonly ILogger<CommercialApprovalController> _logger;
        private readonly IFileStorageManager _fileStorageManager;

        public CommercialApprovalController(
            ICommercialApprovalService approvalService,
            ILogger<CommercialApprovalController> logger,
            IFileStorageManager fileStorageManager)
        {
            _approvalService    = approvalService;
            _logger             = logger;
            _fileStorageManager = fileStorageManager;
        }

        [HttpPost("CommercialApproval/GenerateApprovalXml")]
        public async Task<IActionResult> GenerateApprovalXml([FromBody] ACECF model)
        {
            try
            {
                _logger.LogInformation("Request received to generate commercial approval XML");

                var dgiiResponse = await _approvalService.GenerateAndSignXmlAsync(model);
                var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(dgiiResponse);

                return Ok(new
                {
                    success  = true,
                    estado   = responseData!["codigo"].ToString() == "01" ? 1 : 2,
                    mensaje  = responseData["estado"].ToString(),
                    detalles = responseData["mensaje"] as List<string> ?? new List<string>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating commercial approval XML");
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Error interno al generar el XML de aprobación comercial."
                });
            }
        }

        // WARNING: Do NOT change this route — DGII posts commercial approvals to this exact URL.
        [HttpPost("/fe/aprobacioncomercial/api/ecf")]
        public async Task<IActionResult> ReceiveCommercialApproval(IFormFile xml)
        {
            _logger.LogInformation("Incoming commercial approval from DGII");

            if (xml == null || xml.Length == 0)
            {
                _logger.LogWarning("XML file not provided or empty");
                return BadRequest("Insatisfactorio: archivo XML no proporcionado o vacío.");
            }

            string xmlContent;
            try
            {
                using var reader = new StreamReader(xml.OpenReadStream());
                xmlContent = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading incoming XML file");
                return BadRequest("error al leer el archivo XML.");
            }

            try
            {
                var result = _approvalService.ValidateApproval(xmlContent);

                if (!result.Success)
                {
                    _logger.LogWarning("Validation failed: {Message}", result.Message);
                    return BadRequest("Insatisfactorio");
                }

                var xmlDoc   = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                var buyerRncNode = xmlDoc.SelectSingleNode("//ACECF/DetalleAprobacionComercial/RNCComprador");
                var encfNode     = xmlDoc.SelectSingleNode("//ACECF/DetalleAprobacionComercial/eNCF");

                if (buyerRncNode == null || encfNode == null)
                {
                    _logger.LogWarning("RNCComprador or eNCF node not found in XML");
                    return BadRequest("Insatisfactorio");
                }

                string buyerRnc  = buyerRncNode.InnerText;
                string encf      = encfNode.InnerText;
                string fileName  = $"ACECF_{buyerRnc}_{encf}.xml";

                await _fileStorageManager.SaveACECFAsync(xmlContent, buyerRnc, fileName);

                _logger.LogInformation("Commercial approval saved: {FileName}", fileName);
                return Ok("Satisfactorio");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during commercial approval validation or save");
                return BadRequest("Insatisfactorio");
            }
        }
    }
}
