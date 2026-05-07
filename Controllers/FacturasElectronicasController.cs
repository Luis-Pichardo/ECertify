using Microsoft.AspNetCore.Mvc;
using System.Xml;
using eCertify.Models;
using eCertify.Interfaces;
using Microsoft.EntityFrameworkCore;
using eCertify.Utils;
using Microsoft.Extensions.Logging;
using eCertify.Data;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class FacturasElectronicasController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _authenticationService;
        private readonly IFacturasElectronicasService _facturasElectronicasService;
        private readonly ISemillaService _semillaService;
        private readonly ILogger<FacturasElectronicasController> _logger;
        private readonly SogeDbContext _context;
        private readonly IFileStorageManager _fileStorageManager;
        public FacturasElectronicasController(HttpClient httpClient, IAuthenticationService authenticationService, IFacturasElectronicasService facturasElectronicasService, ISemillaService semillaService, ILogger<FacturasElectronicasController> logger, SogeDbContext context, IFileStorageManager fileStorageManager)
        {
            _httpClient = httpClient;
            _authenticationService = authenticationService;
            _facturasElectronicasService = facturasElectronicasService;
            _semillaService = semillaService;
            _logger = logger;
            _context = context;
            _fileStorageManager = fileStorageManager;
        }

        [HttpGet("GenerarFE/{nombreXml}")]
        public async Task<IActionResult> GenerarFacturaElectronica(string nombreXml)
        {
            if (string.IsNullOrWhiteSpace(nombreXml) || Path.GetInvalidFileNameChars().Any(nombreXml.Contains))
                return BadRequest("Nombre de archivo inválido.");

            try
            {
                string facturasBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Facturas");
                string[] matchingFiles = Directory.GetFiles(facturasBasePath, nombreXml, SearchOption.AllDirectories);

                if (matchingFiles.Length == 0)
                    return NotFound($"No se encontró el archivo {nombreXml} en la carpeta Facturas.");

                string xmlFilePath = matchingFiles[0];
                string rnc = new DirectoryInfo(Path.GetDirectoryName(xmlFilePath)!).Name;

                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == rnc);
                if (empresa == null)
                    return NotFound($"No se encontró la empresa con RNC {rnc}");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                XmlDocument xmlFirmado = _semillaService.FirmarXml(xmlDoc, empresa);
                xmlFirmado.Save(xmlFilePath);

                string token = await _authenticationService.GetAuthToken();
                if (string.IsNullOrEmpty(token))
                    return StatusCode(500, "No se pudo obtener el Bearer Token");

                string trackId = await _facturasElectronicasService.EnviarFactura(xmlFirmado, nombreXml, token);
                if (string.IsNullOrEmpty(trackId))
                    return StatusCode(500, "Error al obtener el trackId");

                var datosFactura = await _facturasElectronicasService.ConsultarFacturaEnviada(trackId, token);

                return Ok(new { Message = "Proceso completado", ResultadosFactura = datosFactura });
            }
            catch (HttpRequestException httpEx)
            {
                return StatusCode(500, $"Error HTTP: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        [HttpPost("firmar-factura-desde-xml")]
        public async Task<IActionResult> GenerarFacturaDesdeXml([FromForm] IFormFile archivoXml)
        {
            if (archivoXml == null || archivoXml.Length == 0)
                return BadRequest("Debes cargar un archivo XML válido.");

            // Leer contenido XML
            string contenidoXml;
            using (var reader = new StreamReader(archivoXml.OpenReadStream()))
            {
                contenidoXml = await reader.ReadToEndAsync();
            }

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(contenidoXml);
            }
            catch (Exception ex)
            {
                return BadRequest($"El archivo XML es inválido: {ex.Message}");
            }
            // Extraer RNC desde el XML (ajusta XPath a tu estructura real)
            string rncEmisor = ""; //xmlDoc.SelectSingleNode("//RNCEmisor")?.InnerText;
            try
            {
                // Extraer RNC desde el XML (ajusta XPath a tu estructura real)
                rncEmisor = xmlDoc.SelectSingleNode("//RNCEmisor")?.InnerText;

                if (string.IsNullOrEmpty(rncEmisor))
                    return BadRequest("No se pudo obtener el RNC del emisor desde el XML.");
                rncEmisor = Utils.Utils.LimpiarRNC(rncEmisor);
            }
            catch (Exception ex)
            {
                return BadRequest($"No se pudo sacar el rnc del emisor del archivo XML: {ex.Message}");
                throw;
            }


            // Buscar empresa
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == rncEmisor);
            if (empresa == null)
                return NotFound($"No se encontró el RNC de la {rncEmisor}");

            XmlDocument xmlFirmado;
            try
            {
                // Firmar el XML con los datos de la empresa
                xmlFirmado = _semillaService.FirmarXml(xmlDoc, empresa, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firmando el XML para RNC {rnc}", rncEmisor);
                return StatusCode(500, $"Error firmando el XML: {ex.Message}");
            }

            // Guardar XML firmado en Storage/Facturas/{rncEmisor}/facturaEmpresa
            string nombreArchivoFirmado = archivoXml.FileName;  //Path.GetFileNameWithoutExtension(archivoXml.FileName) ;
            try
            {
                // Guardar XML firmado en disco con FileStorageManager
                await _fileStorageManager.SaveXmlAsync(rncEmisor, xmlFirmado.OuterXml, nombreArchivoFirmado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando XML firmado para RNC {rnc}", rncEmisor);
                return StatusCode(500, $"Error guardando el XML firmado: {ex.Message}");
            }

            // Obtener token para enviar factura
            string token;
            try
            {
                token = await _semillaService.ObtenerTokenAsync(rncEmisor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo token para RNC {rnc}", rncEmisor);
                return StatusCode(500, $"Error obteniendo token: {ex.Message}");
            }

            // Enviar factura (implementa este método en SemillaService o donde corresponda)
            string trackId;
            try
            {
                trackId = await _facturasElectronicasService.EnviarFactura(xmlFirmado, nombreArchivoFirmado, token);
                if (string.IsNullOrEmpty(trackId))
                    return StatusCode(500, "No se pudo obtener el trackId tras enviar la factura.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando factura para RNC {rnc}", rncEmisor);
                return StatusCode(500, $"Error enviando factura: {ex.Message}");
            }

            // Consultar estado factura (implementa en SemillaService o donde tengas la lógica)
            var resultadoConsulta = await _facturasElectronicasService.ConsultarFacturaEnviada(trackId, token);

            return Ok(new
            {
                Mensaje = "Factura procesada con éxito",
                RNC = rncEmisor,
                ArchivoFirmado = nombreArchivoFirmado,
                Resultado = resultadoConsulta
            });
        }
        [HttpPost("firmar-facturaE32-desde-xml")]
        public async Task<IActionResult> GenerarFacturaE32DesdeXml([FromForm] IFormFile archivoXml)
        {
            if (archivoXml == null || archivoXml.Length == 0)
                return BadRequest("Debes cargar un archivo XML válido.");

            string contenidoXml;
            using (var reader = new StreamReader(archivoXml.OpenReadStream()))
            {
                contenidoXml = await reader.ReadToEndAsync();
            }

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(contenidoXml);
            }
            catch (Exception ex)
            {
                return BadRequest($"El archivo XML es inválido: {ex.Message}");
            }

            string rncEmisor = xmlDoc.SelectSingleNode("//RNCEmisor")?.InnerText?.Trim();
            if (string.IsNullOrEmpty(rncEmisor))
                return BadRequest("No se pudo obtener el RNC del emisor desde el XML.");

            rncEmisor = Utils.Utils.LimpiarRNC(rncEmisor);

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == rncEmisor);
            if (empresa == null)
                return NotFound($"No se encontró la empresa con RNC {rncEmisor}.");

            XmlDocument xmlFirmado;
            try
            {
                xmlFirmado = _semillaService.FirmarXml(xmlDoc, empresa, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firmando el XML para RNC {rnc}", rncEmisor);
                return StatusCode(500, $"Error firmando el XML: {ex.Message}");
            }

            string nombreArchivoFirmado = archivoXml.FileName;
            try
            {
                await _fileStorageManager.SaveXmlAsync(rncEmisor, xmlFirmado.OuterXml, nombreArchivoFirmado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando XML firmado para RNC {rnc}", rncEmisor);
                return StatusCode(500, $"Error guardando el XML firmado: {ex.Message}");
            }

            return Ok(new
            {
                message = "XML firmado y guardado correctamente.",
                fileName = nombreArchivoFirmado
            });
        }


        //[HttpGet("Consultas/Estatus")]
        //public async Task<IActionResult> ConsultarEstatusFactura([FromQuery] string trackId)
        //{
        //    if (string.IsNullOrWhiteSpace(trackId))
        //        return BadRequest("Debe proporcionar un trackId.");

        //    try
        //    {
        //        // Obtener el token
        //        string token = await _semillaService.ObtenerTokenAsync("132650761");
        //        if (string.IsNullOrEmpty(token))
        //            return StatusCode(500, "No se pudo obtener el Bearer Token");

        //        var datosFactura = await _facturasElectronicasService.ConsultarFacturaEnviada(trackId, token);

        //        return Ok(new { Message = "Proceso completado", ResultadosFactura = datosFactura });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error al consultar el estado de la factura con trackId {trackId}", trackId);
        //        return StatusCode(500, $"Error interno: {ex.Message}");
        //    }
        //}

        [HttpGet("Consultas/Estatus")]
        public async Task<IActionResult> ConsultarEstatusFactura(
    [FromQuery] string trackId,
    [FromQuery] string rnc)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                return BadRequest("Debe proporcionar un trackId.");

            if (string.IsNullOrWhiteSpace(rnc))
                return BadRequest("Debe proporcionar un RNC.");

            try
            {
                // Obtener el token usando el RNC proporcionado
                string token = await _semillaService.ObtenerTokenAsync(rnc);
                if (string.IsNullOrEmpty(token))
                    return StatusCode(500, "No se pudo obtener el Bearer Token");

                // Llamada a la DGII
                var datosFactura = await _facturasElectronicasService.ConsultarFacturaEnviada(trackId, token);

                // Verificación y logging
                if (datosFactura == null)
                {
                    _logger.LogWarning("No se obtuvo respuesta válida para el trackId {trackId}", trackId);
                    return NotFound(new
                    {
                        Message = "No se encontró información para el TrackId especificado.",
                        TrackId = trackId
                    });
                }

                _logger.LogInformation("Consulta exitosa para TrackId {trackId}: Estado={estado}, Codigo={codigo}",
                    datosFactura.TrackId, datosFactura.Estado, datosFactura.Codigo);

                return Ok(new
                {
                    Message = "Proceso completado",
                    DatosFactura = new
                    {
                        datosFactura.TrackId,
                        datosFactura.Codigo,
                        datosFactura.Estado,
                        datosFactura.Rnc,
                        datosFactura.Encf,
                        datosFactura.SecuenciaUtilizada,
                        datosFactura.FechaRecepcion,
                        Mensajes = datosFactura.Mensajes?.Select(m => new
                        {
                            m.Codigo,
                            m.Valor
                        })

                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el estado de la factura con trackId {trackId}", trackId);
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


    }
}
