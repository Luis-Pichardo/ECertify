using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using eCertify.Interfaces;
using eCertify.Models.ResumenesModel;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using eCertify.Services;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using eCertify.Models;
using eCertify.Utils;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SimulacionController : ControllerBase
    {
        private readonly IFileStorageManager _fileStorageManager;
        private readonly IFacturasXmlService _facturasXmlService;
        private readonly ISemillaService _semillaService;
        private readonly IEmpresaService _empresaService;
        private readonly ILogger<FacturasXmlController> _logger;
        private readonly IResumenesXmlService _resumenesXmlService;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;

        public SimulacionController(
            IFileStorageManager fileStorageManager,
            IFacturasXmlService facturasXmlService,
            ISemillaService semillaService,
            IEmpresaService empresaService,
            ILogger<FacturasXmlController> logger,
            IResumenesXmlService resumenesXmlService,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            HttpClient httpClient)
        {
            _fileStorageManager = fileStorageManager;
            _facturasXmlService = facturasXmlService;
            _semillaService = semillaService;
            _empresaService = empresaService;
            _logger = logger;
            _resumenesXmlService = resumenesXmlService;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClient;
        }

        // Endpoint para recibir un JSON con datos de una factura ECF, validar dicha información,
        // generar el XML correspondiente, firmarlo digitalmente y almacenarlo en el sistema de archivos.
        // Retorna la información del archivo generado o los errores de validación o procesamiento ocurridos.
        [HttpPost("GenerarFacturasDesdeJson")]
        [Consumes("application/json")]
        public async Task<IActionResult> GenerarXmlDesdeECF([FromBody] FacturaECFRequest request)
        {
            try
            {

                // Validación básica del request
                if (request?.ECF == null)
                {
                    return BadRequest(new { success = false, message = "El cuerpo de la solicitud no puede estar vacío" });
                }

                // Manejo de TablaFormasPago - versión corregida
                if (request.ECF.Encabezado?.IdDoc != null)
                {
                    if (request.ECF.Encabezado.IdDoc.TablaFormasPago == null)
                    {
                        request.ECF.Encabezado.IdDoc.TablaFormasPago = new List<eCertify.Models.FormaDePago>();
                    }
                }

                // Validación de campos obligatorios
                if (request.ECF.Encabezado?.IdDoc?.eNCF == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "NCF inválido o faltante",
                        solucion = "Asegúrese de incluir el campo eNCF en IdDoc"
                    });
                }

                var factura = request.ECF;
                var rncEmisor = factura.Encabezado?.Emisor?.RNCEmisor;

                if (string.IsNullOrWhiteSpace(rncEmisor))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "RNC del emisor es requerido",
                        campo = "ECF.Encabezado.Emisor.RNCEmisor"
                    });
                }

                var rncLimpio = rncEmisor.Trim().Replace("-", "").Replace(" ", "");

                if (string.IsNullOrEmpty(rncLimpio))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "RNC inválido.",
                        rncRecibido = rncEmisor,
                        rncLimpio = rncLimpio
                    });
                }

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

                var ncf = factura.Encabezado.IdDoc.eNCF;
                var tipoFactura = ncf.Length >= 3 ? ncf[..3] : "OTRO";

                // Generación del XML
                var xmlString = await _facturasXmlService.GenerarXmlDesdeModeloAsync(factura);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);

                // Firma del XML
                var xmlFirmado = _semillaService.FirmarXml(xmlDoc, empresa, true);

                // Guardado del archivo
                var fileName = $"{rncLimpio}{ncf}.xml";
                var path = await _fileStorageManager.SaveSimulacionXmlAsync(rncLimpio, tipoFactura, xmlFirmado.OuterXml, fileName);

                //Generar la Representacion impresa de los XMLs:
                var client = _httpClientFactory.CreateClient("ApiClient");
                string endpointPdf = "/api/RepresentacionImpresa/generar-pdf";

                decimal montoTotalDecimal = 0;
                var tipoeCF = factura.Encabezado?.IdDoc?.TipoeCF;
                var montoTotalStr = factura.Encabezado?.Totales?.MontoTotal;
                var parseoExitoso = decimal.TryParse(montoTotalStr, out montoTotalDecimal);

                _logger.LogInformation(
                    "[PDF] Iniciando generación de PDF | NCF: {ncf} | TipoeCF: {tipoeCF} | MontoTotal (raw): '{montoTotalStr}' | ParseOk: {parseoExitoso} | MontoDecimal: {montoTotalDecimal}",
                    ncf, tipoeCF, montoTotalStr, parseoExitoso, montoTotalDecimal);

                if (tipoeCF == "32" && parseoExitoso && montoTotalDecimal < 250000)
                {
                    endpointPdf = "/api/RepresentacionImpresa/generar-pdf-E32";
                }

                _logger.LogInformation(
                    "[PDF] Endpoint seleccionado: {endpointPdf} | BaseAddress cliente: {baseAddress}",
                    endpointPdf, client.BaseAddress);

                var archivoPath = Path.Combine("Storage", "Certificacion", "Simulacion", "Facturas", rncLimpio, tipoFactura, fileName);
                var archivoExiste = System.IO.File.Exists(archivoPath);
                _logger.LogInformation("[PDF] Ruta del archivo XML: {archivoPath} | Existe: {archivoExiste}", archivoPath, archivoExiste);

                if (!archivoExiste)
                {
                    _logger.LogError("[PDF] El archivo XML no existe en la ruta esperada: {archivoPath}", archivoPath);
                }

                using var stream = System.IO.File.OpenRead(archivoPath);
                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "archivoXml", fileName);

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(endpointPdf, content);
                }
                catch (Exception pdfEx)
                {
                    _logger.LogError(pdfEx,
                        "[PDF] Excepción al llamar al endpoint {endpointPdf} para NCF {ncf}. BaseAddress: {baseAddress}",
                        endpointPdf, ncf, client.BaseAddress);
                    throw;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[PDF] Falló la generación del PDF | NCF: {ncf} | Endpoint: {endpointPdf} | StatusCode: {statusCode} | Respuesta: {responseBody}",
                        ncf, endpointPdf, response.StatusCode, responseBody);
                }
                else
                {
                    _logger.LogInformation(
                        "[PDF] PDF generado correctamente | NCF: {ncf} | Endpoint: {endpointPdf} | StatusCode: {statusCode}",
                        ncf, endpointPdf, response.StatusCode);
                }


                // --- GENERAR RESUMEN SI ES TIPO 32 Y MONTOTOTAL < 250,000 ---
                if (factura.Encabezado?.IdDoc?.TipoeCF == "32" &&
                    decimal.TryParse(factura.Encabezado?.Totales?.MontoTotal, out montoTotalDecimal) &&
                    montoTotalDecimal < 250000)
                {
                    var resumenModel = _resumenesXmlService.MapearResumenDesdeFactura(factura);

                    // Obtener código seguridad desde factura firmada
                    var nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
                    nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                    var signatureValueNode = xmlDoc.SelectSingleNode("//ds:SignatureValue", nsMgr);
                    if (signatureValueNode != null)
                    {
                        var hash = signatureValueNode.InnerText;
                        resumenModel.Encabezado.CodigoSeguridadeCF = hash.Substring(0, 6);
                    }

                    // Crear XML resumen y firmar
                    var resumenXmlDoc = CrearResumenDesdeModelo(resumenModel);
                    var resumenFirmado = _semillaService.FirmarXml(resumenXmlDoc, empresa, false);

                    // Insertar Signature justo después de Encabezado
                    var rfceNode = resumenFirmado.SelectSingleNode("/RFCE");
                    var encabezadoNode = resumenFirmado.SelectSingleNode("/RFCE/Encabezado");
                    var signatureNode = resumenFirmado.SelectSingleNode("//Signature", nsMgr);
                    if (rfceNode != null && encabezadoNode != null && signatureNode != null)
                    {
                        signatureNode.ParentNode.RemoveChild(signatureNode);
                        rfceNode.InsertAfter(signatureNode, encabezadoNode);
                    }

                    // Guardar resumen
                    var resumenFileName = $"{rncEmisor}{ncf}.xml";
                    await _fileStorageManager.SaveResumenSimulacionAsync(rncEmisor, resumenFirmado.OuterXml, resumenFileName);
                }

                return Ok(new
                {
                    success = true,
                    message = "El archivo XML se generó y firmó correctamente desde el Json recibido.",
                    rnc = rncLimpio,
                    empresa = empresa.RazonSocial,
                    ncf,
                    path,
                    fileName,
                    fechaGeneracion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización JSON en GenerarXmlDesdeECF");
                return BadRequest(new
                {
                    success = false,
                    message = "Error en el formato de los datos JSON",
                    detalle = jsonEx.Message,
                    solucion = "Verifique especialmente el formato de TablaFormasPago (debe ser un array)"
                });
            }
            catch (XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "Error al generar XML en GenerarXmlDesdeECF");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar el XML",
                    detalle = xmlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GenerarXmlDesdeECF");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("GenerarResumenDesdeJson")]
        [Consumes("application/json")]
        public async Task<IActionResult> GenerarXmlResumenDesdeJson([FromBody] ResumenECFRequest request)
        {
            try
            {
                // Validación básica
                if (request?.RFCE == null)
                {
                    return BadRequest(new { success = false, message = "El cuerpo de la solicitud no puede estar vacío" });
                }

                var rnc = request.RFCE?.Encabezado?.Emisor?.RNCEmisor?.Trim().Replace("-", "").Replace(" ", "");
                if (string.IsNullOrWhiteSpace(rnc))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "RNC del emisor es requerido",
                        campo = "Resumen.Encabezado.Emisor.RNCEmisor"
                    });
                }

                // Buscar empresa
                var empresa = await _empresaService.GetEmpresaByRncAsync(rnc);
                if (empresa == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Empresa con RNC {rnc} no encontrada.",
                        solucion = "Verifique el RNC o registre la empresa primero."
                    });
                }

                // Serializar el modelo a XML
                var xmlDoc = CrearResumenDesdeModelo(request.RFCE);

                // Firmar con fecha
                var xmlFirmado = _semillaService.FirmarXml(xmlDoc, empresa, true);

                // Guardado del archivo
                var eNCF = request.RFCE?.Encabezado?.IdDoc?.eNCF?.Trim();
                if (string.IsNullOrWhiteSpace(eNCF))
                {
                    return BadRequest(new { success = false, message = "El campo eNCF es obligatorio en el resumen." });
                }

                var fileName = $"{rnc}{eNCF}.xml";
                var tipo = Path.Combine("Facturas", "Resumenes");
                var path = await _fileStorageManager.SaveResumenSimulacionAsync(rnc, xmlFirmado.OuterXml, fileName);

                return Ok(new
                {
                    success = true,
                    message = "El archivo XML resumen se generó y firmó correctamente.",
                    rnc,
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

        [HttpPost("EnviarfacturasDGII")]
        public async Task<IActionResult> EnviarXmlADgii([FromForm] string rnc, [FromForm] string fileName)
        {
            try
            {
                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".xml";
                }

                if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, message = "Nombre de archivo inválido o no es un archivo XML." });

                var nombreConRnc = rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml";

                var tipoFactura = fileName.Split('_')[0];
                if (!fileName.Contains("_"))
                {
                    tipoFactura = fileName.Substring(0, 3);
                }

                var xmlPath = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Simulacion", "Facturas", rnc, tipoFactura, nombreConRnc);
                if (!System.IO.File.Exists(xmlPath))
                    return NotFound(new { success = false, message = "Archivo XML no encontrado." });

                var token = await _semillaService.ObtenerTokenAsync(rnc);

                using var formContent = new MultipartFormDataContent();
                var fileBytes = await System.IO.File.ReadAllBytesAsync(xmlPath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");
                formContent.Add(fileContent, "xml", nombreConRnc);

                var client = _httpClientFactory.CreateClient("ApiClient");
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = formContent;

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al enviar XML a DGII: {Status} - {Content}", response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { success = false, message = errorContent });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var trackInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (!trackInfo.TryGetProperty("trackId", out var trackIdElement))
                    return StatusCode(502, new { success = false, message = "La respuesta de la DGII no contiene trackId." });

                var trackId = trackIdElement.GetString();
                await Task.Delay(5000);

                //var consultaUrl = $"api/FacturasElectronicas/Consultas/Estatus?rnc={rnc}&trackId={trackId}";

                //var consultaResponse = await client.GetAsync(consultaUrl);
                //consultaResponse.EnsureSuccessStatusCode();
                //var estadoJson = await consultaResponse.Content.ReadAsStringAsync();

                //return Content(estadoJson, "application/json");

                var tokenApp = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrWhiteSpace(tokenApp))
                    return Unauthorized(new { success = false, message = "El token de acceso de la aplicación no fue proporcionado." });

                var consultaUrl = $"api/FacturasElectronicas/Consultas/Estatus?rnc={rnc}&trackId={trackId}";

                using var consultaRequest = new HttpRequestMessage(HttpMethod.Get, consultaUrl);
                consultaRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenApp);

                var consultaResponse = await client.SendAsync(consultaRequest);
                consultaResponse.EnsureSuccessStatusCode();
                var estadoJson = await consultaResponse.Content.ReadAsStringAsync();

                return Content(estadoJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar XML a la DGII para RNC {RNC}", rnc);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("EnviarResumenADGII")]
        public async Task<IActionResult> EnviarResumenADgii([FromForm] string rnc, [FromForm] string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rnc) || string.IsNullOrWhiteSpace(fileName))
                {
                    return BadRequest(new { success = false, message = "El RNC y el nombre del archivo son requeridos." });
                }

                // Asegurar extensión .xml
                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    fileName += ".xml";

                // Concatenar RNC al nombre del archivo como fue guardado previamente
                var nombreArchivoCompleto = rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml";
                // Ruta física del archivo firmado de resumen
                var xmlPath = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Simulacion", "Resumenes", rnc, nombreArchivoCompleto);
                if (!System.IO.File.Exists(xmlPath))
                {
                    return NotFound(new { success = false, message = "Archivo XML de resumen no encontrado." });
                }

                // Obtener token de seguridad de la DGII usando el RNC
                var token = await _semillaService.ObtenerTokenAsync(rnc);

                // Crear contenido del formulario con el archivo
                using var formContent = new MultipartFormDataContent();
                var fileBytes = await System.IO.File.ReadAllBytesAsync(xmlPath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");
                formContent.Add(fileContent, "xml", nombreArchivoCompleto);

                // Crear y configurar request HTTP
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://ecf.dgii.gov.do/CerteCF/recepcionfc/api/recepcion/ecf");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = formContent;

                // Enviar request
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al enviar resumen XML a DGII: {Status} - {Content}", response.StatusCode, responseContent);
                    return StatusCode((int)response.StatusCode, new { success = false, message = responseContent });
                }

                // Retornar respuesta tal como la entrega la DGII
                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar resumen XML a la DGII para RNC {RNC}", rnc);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("EnviarXmlSegunTipo")]
        public async Task<IActionResult> EnviarXmlSegunTipo([FromForm] string rnc, [FromForm] string fileName)
        {
            try
            {
                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    fileName += ".xml";

                // Rutas donde buscar el XML (facturas o resúmenes)
                var tipoFactura = fileName.Substring(0, 3);
                var rutaFactura = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Simulacion", "Facturas", rnc, tipoFactura, rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml");
                var rutaResumen = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Simulacion", "Resumenes", rnc, rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml");

                XmlDocument xmlDoc = new XmlDocument();
                if (System.IO.File.Exists(rutaFactura))
                    xmlDoc.Load(rutaFactura);
                else if (System.IO.File.Exists(rutaResumen))
                    xmlDoc.Load(rutaResumen);
                else
                    return NotFound(new { success = false, message = "Archivo XML no encontrado." });

                // Extraer tipo y monto según estructura XML
                var tipoNodo = xmlDoc.SelectSingleNode("//Encabezado/IdDoc/TipoeCF");
                var montoNodo = xmlDoc.SelectSingleNode("//Encabezado/Totales/MontoTotal");

                if (tipoNodo == null || montoNodo == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No se pudo determinar el tipo o monto del XML.",
                        tipoNodo = tipoNodo?.InnerText,
                        montoNodo = montoNodo?.InnerText
                    });
                }

                var tipo = tipoNodo.InnerText.Trim();
                if (!decimal.TryParse(montoNodo.InnerText, out decimal monto))
                    return BadRequest(new { success = false, message = "Monto inválido en el XML." });

                // Lógica para decidir si es factura o resumen:
                if (tipo == "32" && monto < 250000)
                {
                    // Enviar como resumen B2C
                    return await EnviarResumenADgii(rnc, fileName);
                }
                else
                {
                    // Enviar como factura (ECF)
                    return await EnviarXmlADgii(rnc, fileName);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EnviarXmlSegunTipo para RNC {RNC}", rnc);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("DescargarXml")]
        public IActionResult DescargarXml([FromQuery] string rnc, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(rnc) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest("RNC, tipo de factura y nombre de archivo son requeridos");

            try
            {
                string tipoFactura = fileName.Length >= 3 ? fileName.Substring(0, 3) : null;
                if (string.IsNullOrEmpty(tipoFactura))
                    return BadRequest("El nombre del archivo no contiene tipo de factura válido");
                fileName = rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml";
                string rutaArchivo = Path.Combine(_fileStorageManager.GetBaseStoragePath(), "Certificacion", "Simulacion", "Facturas", rnc, tipoFactura, fileName);

                if (!System.IO.File.Exists(rutaArchivo))
                    return NotFound("Archivo no encontrado");

                string xmlContent = System.IO.File.ReadAllText(rutaArchivo);
                var factura = XmlUtils.DeserializarXml<FacturasModels>(xmlContent);

                string tipoECF = factura?.Encabezado?.IdDoc?.TipoeCF ?? "";
                string montoTotalStr = factura?.Encabezado?.Totales?.MontoTotal ?? "0";

                decimal montoTotal = 0;
                decimal.TryParse(montoTotalStr, out montoTotal);

                if (tipoECF != "32" || montoTotal >= 250000)
                    return Forbid("No autorizado para descargar este archivo");

                var fileBytes = System.IO.File.ReadAllBytes(rutaArchivo);
                return File(fileBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar archivo XML {FileName} para RNC {Rnc}", fileName, rnc);
                return StatusCode(500, "Error interno al procesar la descarga");
            }
        }

        private XmlDocument CrearResumenDesdeModelo(ResumenesModel model)
        {
            var serializer = new XmlSerializer(typeof(ResumenesModel));

            // Configuración para UTF-8 sin BOM
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), // false = sin BOM
                Indent = true,
                OmitXmlDeclaration = false // Mantiene la declaración XML
            };

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", ""); // Elimina namespaces por defecto

            using var memoryStream = new MemoryStream();
            using (var writer = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(writer, model, namespaces);
            }

            var xmlDoc = new XmlDocument();
            memoryStream.Position = 0; // Rebobinar el stream
            xmlDoc.Load(memoryStream);

            return xmlDoc;
        }


        private XmlDocument SerializarResumenAXml(ResumenesModel model)
        {
            var serializer = new XmlSerializer(typeof(ResumenesModel));

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), // UTF-8 sin BOM
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var memoryStream = new MemoryStream();
            using (var writer = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(writer, model);
            }

            var xmlDoc = new XmlDocument();
            memoryStream.Position = 0;
            xmlDoc.Load(memoryStream);

            return xmlDoc;
        }
    }
}
