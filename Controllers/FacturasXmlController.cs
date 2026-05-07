/*********************************************************************
 *                        DESARROLLADOR ENCARGADO:                    *
 *                             Luís Pichardo                         *
 *                                                                   *
 * Código desarrollado y mantenido por Luís Pichardo, quien es el   *
 * responsable principal de esta implementación.                     *
 *********************************************************************/


using Microsoft.AspNetCore.Mvc;
using eCertify.Interfaces;
using System.Xml;
using eCertify.Models;
using Microsoft.IdentityModel.Tokens;
using eCertify.Models.ResumenesModel;
using System.Text;
using System.Xml.Serialization;
using System.Net.Http.Headers;
using System.Net.Http;
using eCertify.Utils;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System.Globalization;
using eCertify.DTOs;
using System.Text.Json;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FacturasXmlController : ControllerBase
    {
        private readonly IFileStorageManager _fileStorageManager;
        private readonly IFacturasXmlService _facturasXmlService;
        private readonly IResumenesXmlService _resumenesXmlService;
        private readonly ISemillaService _semillaService;
        private readonly IEmpresaService _empresaService;
        private readonly ILogger<FacturasXmlController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SogeDbContext _dbContext;

        public FacturasXmlController(
            IFileStorageManager fileStorageManager,
            IFacturasXmlService facturasXmlService,
            IResumenesXmlService resumenesXmlService,
            ISemillaService semillaService,
            IEmpresaService empresaService,
            ILogger<FacturasXmlController> logger,
            IHttpClientFactory httpClientFactory,
            SogeDbContext dbContext)
        {
            _fileStorageManager = fileStorageManager;
            _facturasXmlService = facturasXmlService;
            _resumenesXmlService = resumenesXmlService;
            _semillaService = semillaService;
            _empresaService = empresaService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        [HttpPost("Produccion/GenerarFactura")]
        [Consumes("application/json")]
        public async Task<IActionResult> GenerarFacturas([FromBody] FacturaECFRequest request)
        {
            try
            {
                var factura = request.ECF;
                var tipoECF = factura.Encabezado?.IdDoc?.TipoeCF;
                var montoTotal = Utils.Utils.ParseDecimalOrDefault(factura.Encabezado?.Totales?.MontoTotal);
                var rncEmisor = Utils.Utils.LimpiarRNC(factura.Encabezado.Emisor?.RNCEmisor);
                var rncComprador = Utils.Utils.LimpiarRNC(factura.Encabezado.Comprador?.RNCComprador ?? factura.Encabezado.Comprador?.IdentificadorExtranjero);
                var RazonSocialComprador = factura.Encabezado.Comprador?.RazonSocialComprador;

                // 1. Generar y firmar factura
                var facturaResult = await GenerarXmlFacturaDesdeJson(request);
                if (facturaResult is not OkObjectResult facturaOk)
                    return facturaResult;

                var facturaData = facturaOk.Value as dynamic;
                var fechaHoraFirma = facturaData?.fechaHoraFirma;
                var codigoSeguridad = facturaData?.codigoSeguridad;
                var info = new ExtraInfoFacturaDTO
                {
                    CodigoSeguridad = codigoSeguridad,
                    FechaHoraFirma = DateTime.Parse(fechaHoraFirma)
                };
                var urlQR = _facturasXmlService.GenerarUrlQR(factura, info);
                // 2. Si tipo 32 y monto < 250,000 → generar resumen y usarlo
                if (tipoECF == "32" && montoTotal < 250000)
                {
                    var resumenModel = _resumenesXmlService.MapearResumenDesdeFactura(factura);
                    resumenModel.Encabezado.CodigoSeguridadeCF = codigoSeguridad;

                    var resumenRequest = new ResumenECFRequest { RFCE = resumenModel };
                    
                    // Generar y firmar resumen
                    var resumenResult = await GenerarXmlResumenDesdeJson(resumenRequest);
                    if (resumenResult is not OkObjectResult resumenOk)
                        return resumenResult;

                    var firmadoXmlResumen = await _fileStorageManager.GetResumenXmlAsync(resumenModel.Encabezado.Emisor.RNCEmisor, resumenModel.Encabezado.IdDoc.eNCF);
                    var nombreArchivo = $"{resumenModel.Encabezado.Emisor.RNCEmisor}{resumenModel.Encabezado.IdDoc.eNCF}.xml";

                    var envioResult = await _facturasXmlService.EnviarFacturaADGIIAsync(
                        firmadoXmlResumen,
                        nombreArchivo,
                        tipoECF,
                        montoTotal,
                        rncEmisor
                    );

                    if (!envioResult.exito)
                        return BadRequest(new { success = false, message = envioResult.mensaje });

                    Task.Delay(5000).Wait(); // Esperar 5 segundos para evitar problemas de sincronización
                    var respuestaJson = await _facturasXmlService.ConsultarEstadoDGIIAsync(envioResult.trackId, rncEmisor);
                    var respuestaDGII = JsonConvert.DeserializeObject<DgiiResponseDto>(respuestaJson);
                    respuestaDGII.TrackIdDgii = envioResult.trackId;

                    await _facturasXmlService.GuardarConsultasFacturaAsync(new FacturaResponseDto
                    {
                        CodigoSeguridad = codigoSeguridad,
                        FechaHoraFirma = fechaHoraFirma,
                        UrlQR = urlQR,
                        RncComprador = rncComprador,
                        RazonSocialComprador = RazonSocialComprador,
                        RespuestaDGII = respuestaDGII
                    });

                    return Ok(new
                    {
                        success = true,
                        message = "Resumen generado, enviado y consultado correctamente",
                        urlQR,
                        fechaHoraFirma,
                        codigoSeguridad,
                        respuestaDGII
                    });
                }

                // 3. Si no es resumen, continuar con envío de factura
                var firmadoXml = await _fileStorageManager.GetFacturaXmlAsync(rncEmisor, tipoECF, factura.Encabezado.IdDoc.eNCF);
                var nombreArchivoFactura = $"{rncEmisor}{factura.Encabezado.IdDoc.eNCF}.xml";

                var envioFactura = await _facturasXmlService.EnviarFacturaADGIIAsync(
                    firmadoXml,
                    nombreArchivoFactura,
                    tipoECF,
                    montoTotal,
                    rncEmisor
                );

                if (!envioFactura.exito)
                    return BadRequest(new { success = false, message = envioFactura.mensaje });

                Task.Delay(5000).Wait(); // Esperar 5 segundo para evitar problemas de sincronización
                var respuestaFacturaJson = await _facturasXmlService.ConsultarEstadoDGIIAsync(envioFactura.trackId, rncEmisor);
                var respuestaDGIIFactura = JsonConvert.DeserializeObject<DgiiResponseDto>(respuestaFacturaJson);
                respuestaDGIIFactura.TrackIdDgii = envioFactura.trackId;

                await _facturasXmlService.GuardarConsultasFacturaAsync(new FacturaResponseDto
                {
                    CodigoSeguridad = codigoSeguridad,
                    FechaHoraFirma = fechaHoraFirma,
                    UrlQR = urlQR,
                    RncComprador = rncComprador,
                    RazonSocialComprador = RazonSocialComprador,
                    RespuestaDGII = respuestaDGIIFactura
                });

                return Ok(new
                {
                    success = true,
                    message = "Factura generada, enviada y consultada correctamente",
                    urlQR,
                    fechaHoraFirma,
                    codigoSeguridad,
                    respuestaDGII = respuestaDGIIFactura
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GenerarFacturaYResumenSiAplica");
                return StatusCode(500, new { success = false, message = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpGet("Produccion/Consultar/Facturas")]
        public async Task<IActionResult> ObtenerHistorialFacturas([FromQuery] string rnc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rnc))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El RNC es requerido"
                    });
                }

                var historial = await _dbContext.HistorialFacturas
                    .Where(h => h.RncEmisor == rnc)
                    .OrderByDescending(h => h.FechaRecepcion)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    total = historial.Count,
                    data = historial
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de facturas");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno al obtener historial de facturas",
                    detalle = ex.Message
                });
            }
        }

        private async Task<IActionResult> GenerarXmlFacturaDesdeJson([FromBody] FacturaECFRequest request)
        {
            try
            {
                if (request.ECF == null)
                {
                    return BadRequest(new { success = false, message = "El cuerpo de la solicitud no puede estar vacío" });
                } 

                var factura = request.ECF;

                // Validar factura
                await _facturasXmlService.ValidarFacturaAsync(factura);

                // Manejo de TablaFormasPago si es necesario
                if (factura.Encabezado?.IdDoc?.TablaFormasPago == null)
                {
                    factura.Encabezado.IdDoc.TablaFormasPago = new List<eCertify.Models.FormaDePago>();
                }

                // Obtener RNC emisor
                var rncEmisor = factura.Encabezado.Emisor.RNCEmisor;
                var rncLimpio = Utils.Utils.LimpiarRNC(rncEmisor);
                var rncComprador = factura.Encabezado.Comprador?.RNCComprador ?? factura.Encabezado.Comprador?.IdentificadorExtranjero;
                var rncCompradorLimpio = Utils.Utils.LimpiarRNC(rncComprador);

                // Buscar empresa
                var empresa = await _empresaService.GetEmpresaByRncAsync(rncLimpio);
                if (empresa == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Empresa con RNC {rncLimpio} no encontrada.",
                        solucion = "Verifique el RNC o registre la empresa primero."
                    });
                }

                // Generar XML
                var xmlString = await _facturasXmlService.GenerarXmlDesdeModeloAsync(factura);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);

                // Firmar XML y extraer información
                var (info, xmlFirmado) = _facturasXmlService.ExtraerInformacionDesdeXml(xmlDoc, empresa);


                // Guardar archivo
                var fileName = $"{rncLimpio}{factura.Encabezado.IdDoc.eNCF}.xml";
                var path = await _fileStorageManager.SaveFacturaXmlAsync(rncLimpio, factura.Encabezado.IdDoc.TipoeCF, xmlFirmado.OuterXml, fileName);

                return Ok(new
                {
                    success = true,
                    message = "XML generado y firmado correctamente",
                    fechaHoraFirma = info.FechaHoraFirma.ToString("dd-MM-yyyy HH:mm:ss"),
                    codigoSeguridad = info.CodigoSeguridad,
                    xml = xmlFirmado.OuterXml
                });

            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización JSON en GenerarXmlDesdeECF");
                return BadRequest(new { success = false, message = "Error en el formato de los datos JSON", detalle = jsonEx.Message });
            }
            catch (XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "Error al generar XML");
                return StatusCode(500, new { success = false, message = "Error al generar el XML", detalle = xmlEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado");
                return StatusCode(500, new { success = false, message = "Error interno del servidor", detalle = ex.Message });
            }
        }

        private async Task<IActionResult> GenerarXmlResumenDesdeJson([FromBody] ResumenECFRequest request)
        {
            try
            {
                if (request?.RFCE == null)
                {
                    return BadRequest(new { success = false, message = "El cuerpo de la solicitud no puede estar vacío" });
                }

                var rncEmisor = Utils.Utils.LimpiarRNC(request.RFCE?.Encabezado?.Emisor?.RNCEmisor);

                if (string.IsNullOrWhiteSpace(rncEmisor))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "RNC del emisor es requerido",
                        campo = "Resumen.Encabezado.Emisor.RNCEmisor"
                    });
                }

                // Buscar empresa
                var empresa = await _empresaService.GetEmpresaByRncAsync(rncEmisor);
                if (empresa == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Empresa con RNC {rncEmisor} no encontrada.",
                        solucion = "Verifique el RNC o registre la empresa primero."
                    });
                }

                // Serializar el modelo a XML
                var xmlDoc = CrearResumenDesdeModelo(request.RFCE);

                // Firmar sin fecha
                var xmlFirmado = _semillaService.FirmarXml(xmlDoc, empresa, false);
                _logger.LogInformation("XML resumen firmado correctamente para RNC: {RncEmisor}", rncEmisor);
                // Guardado del archivo
                var eNCF = request.RFCE?.Encabezado?.IdDoc?.eNCF?.Trim();
                if (string.IsNullOrWhiteSpace(eNCF))
                {
                    return BadRequest(new { success = false, message = "El campo eNCF es obligatorio en el resumen." });
                }

                var fileName = $"{rncEmisor}{eNCF}.xml";
                var tipo = Path.Combine("Facturas", "Resumenes");
                var path = await _fileStorageManager.SaveResumenXmlAsync(rncEmisor, xmlFirmado.OuterXml, fileName);

                return Ok(new
                {
                    success = true,
                    message = "El archivo XML resumen se generó y firmó correctamente.",
                    rncEmisor,
                    empresa = empresa.RazonSocial,
                    fileName,
                    path,
                    fechaGeneracion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización JSON en GenerarXmlResumenDesdeJson");
                return BadRequest(new
                {
                    success = false,
                    message = "Error en el formato de los datos JSON",
                    detalle = jsonEx.Message
                });
            }
            catch (XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "Error al generar XML en GenerarXmlResumenDesdeJson");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar el XML",
                    detalle = xmlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GenerarXmlResumenDesdeJson");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    detalle = ex.Message
                });
            }
        }

        private XmlDocument CrearResumenDesdeModelo(ResumenesModel model)
        {
            var elemento = _resumenesXmlService.CrearXmlDesdeObjeto(model);
            var doc = new XmlDocument();
            using var reader = elemento.CreateReader();
            doc.Load(reader);
            return doc;
        }

    }

    // DTO que representa el cuerpo del request esperado por el endpoint GenerarXmlFacturaDesdeJson.
    // Contiene la factura en formato FacturasModels.
    public class FacturaECFRequest
    {
        public FacturasModels ECF { get; set; }
    }

    // DTO que representa el cuerpo del request esperado por el endpoint GenerarXmlResumenDesdeJson.
    // Contiene el Resumen en formato ResumenModel.
    public class ResumenECFRequest
    {
        public ResumenesModel RFCE { get; set; }
    }


}

