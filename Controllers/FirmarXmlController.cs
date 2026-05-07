using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using eCertify.DTOs;
using eCertify.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Xml;

namespace eCertify.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class FirmarXmlController : ControllerBase
    {
        private readonly SogeDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ISemillaService _semillaService;
        private readonly ILogger<FirmarXmlController> _logger;

        public FirmarXmlController(
            SogeDbContext context,
            IWebHostEnvironment env,
            ISemillaService semillaService,
            ILogger<FirmarXmlController> logger)
        {
            _context        = context;
            _env            = env;
            _semillaService = semillaService;
            _logger         = logger;
        }

        [HttpPost("FirmarXml")]
        public async Task<IActionResult> SignXml([FromForm] FirmarXmlRequestDTO request)
        {
            if (request.XmlArchivo == null || request.XmlArchivo.Length == 0)
            {
                _logger.LogWarning("[SignXml] No XML file received.");
                return BadRequest("No se recibió ningún archivo XML.");
            }

            _logger.LogInformation("[SignXml] Signing request received. RNC={Rnc} File={File}",
                request.RNC, request.XmlArchivo.FileName);

            var company = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == request.RNC);
            if (company == null)
            {
                _logger.LogWarning("[SignXml] Company not found for RNC={Rnc}", request.RNC);
                return NotFound("No se encontró una empresa con el RNC proporcionado.");
            }

            try
            {
                var xmlDoc = new XmlDocument();
                using (var stream = request.XmlArchivo.OpenReadStream())
                    xmlDoc.Load(stream);

                var signedXml = _semillaService.FirmarXml(xmlDoc, company, agregarFechaHora: false);

                byte[] signedBytes;
                using (var ms = new MemoryStream())
                {
                    signedXml.Save(ms);
                    signedBytes = ms.ToArray();
                }

                var fileName = Path.GetFileName(request.XmlArchivo.FileName);
                _logger.LogInformation("[SignXml] File '{File}' signed successfully. Size={Size} bytes",
                    fileName, signedBytes.Length);

                return File(signedBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignXml] Error signing file for RNC={Rnc}", request.RNC);

                if (ex.InnerException is System.Security.Cryptography.CryptographicException)
                {
                    _logger.LogWarning("[SignXml] Certificate password is incorrect for RNC={Rnc}", request.RNC);
                    return UnprocessableEntity("La contraseña del certificado digital es incorrecta. Verifica la contraseña registrada para esta empresa.");
                }

                return StatusCode(500, "Error al procesar la firma digital. Por favor, intenta nuevamente.");
            }
        }
    }
}
