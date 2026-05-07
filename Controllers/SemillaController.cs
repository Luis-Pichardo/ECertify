using Microsoft.AspNetCore.Mvc;
using System.Xml;
using eCertify.Utils;
using Microsoft.Extensions.Options;
using eCertify.Settings;
using eCertify.Interfaces;
using eCertify.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    //[Authorize]
    [Route("/fe/autenticacion/api/[controller]")]
    [ApiController]
  
    public class SemillaController : ControllerBase
    {

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SemillaController> _logger;
        private readonly ISemillaService _semillaService;
        public SemillaController(HttpClient httpClient, IOptions<AppSettings> config, IWebHostEnvironment env, ILogger<SemillaController> logger, ISemillaService semillaService)
        {
            _httpClient = httpClient;
            _baseUrl = config.Value.BaseUrl;
            _env = env;
            _logger = logger;
            _semillaService = semillaService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerToken([FromQuery] string rnc)
        {
            const string endpointName = nameof(ObtenerToken);
            _logger.LogInformation("Iniciando solicitud en {Endpoint} para RNC: {RNC}", endpointName, rnc);
            try
            {
                var token = await _semillaService.ObtenerTokenAsync(rnc);
                _logger.LogInformation("Solicitud completada exitosamente en {Endpoint}", endpointName);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en {Endpoint}: {ErrorMessage}", endpointName, ex.Message);
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }



        [HttpPost]
        [Route("/fe/autenticacion/api/validacioncertificado")]
        public async Task<IActionResult> PostValidacionCertificado()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                string xmlRecibido = await reader.ReadToEndAsync();

                string certificatePath = Path.Combine(_env.ContentRootPath, Constants.CertificatePath);

                string respuestaXml = $@"
                <ValidacionCertificadoResponse xmlns=""https://dgii.gov.do/facturaElectronica/validacionCertificado"">
                  <Estado>OK</Estado>
                  <Mensaje>Certificado validado correctamente</Mensaje>
                  <Fecha>{DateTime.Now:yyyy-MM-ddTHH:mm:ss}</Fecha>
                </ValidacionCertificadoResponse>";

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.LoadXml(respuestaXml);

                XmlDocument xmlFirmado = Utils.Utils.FirmarXml(xmlDoc, certificatePath, Constants.certificatePassword);

                return Content(xmlFirmado.OuterXml, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo en ValidacionCertificado");
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
