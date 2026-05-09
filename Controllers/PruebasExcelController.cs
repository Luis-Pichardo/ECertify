/*********************************************************************
 *                        DESARROLLADOR ENCARGADO:                    *
 *                             Luís Pichardo                         *
 *                                                                   *
 * Código desarrollado y mantenido por Luís Pichardo, quien es el   *
 * responsable principal de esta implementación.                     *
 *********************************************************************/

using Microsoft.AspNetCore.Mvc;
using eCertify.Interfaces;
using eCertify.Utils;
using OfficeOpenXml;
using System.Xml;
using eCertify.Models;
using eCertify.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PruebasExcelController : ControllerBase
    {
        private readonly IFileStorageManager _fileStorageManager;
        private readonly IFacturasXmlService _facturasXmlService;
        private readonly IResumenesXmlService _resumenesXmlService;
        private readonly IWebHostEnvironment _env;
        private readonly ISemillaService _semillaService;
        private readonly IEmpresaService _empresaService;
        private readonly ILogger<PruebasExcelController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public PruebasExcelController(
            IFileStorageManager fileStorageManager,
            IFacturasXmlService facturasXmlService,
            IResumenesXmlService resumenesXmlService,
            ISemillaService semillaService,
            IEmpresaService empresaService,
            ILogger<PruebasExcelController> logger,
            HttpClient httpClient,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory)
        {
            _fileStorageManager = fileStorageManager;
            _facturasXmlService = facturasXmlService;
            _resumenesXmlService = resumenesXmlService;
            _semillaService = semillaService;
            _empresaService = empresaService;
            _logger = logger;
            _httpClient = httpClient;
            _env = env;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        ///Este Endpoint es para listar los XMLs generados por RNC.
        ///Su funcionalidad es cargar los archivos XML de Pruebas de Datos e-CF generados en la carpeta Storage/Certificacion/XMLGenerados/{RNC}
        ///Para poder utilizarlos desde el frontend y permitir al usuario enviarlos a la DGII.
        [HttpGet("ListarXmlsGenerados")]
        public IActionResult ListarXmlsGenerados([FromQuery] string rnc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rnc))
                    return BadRequest(new { success = false, message = "El RNC es requerido" });

                string carpetaXmls = _fileStorageManager.GetDynamicFolderPath(rnc, FileStorageManager.StorageType.Certificacion);

                if (!Directory.Exists(carpetaXmls))
                    return Ok(new { success = true, archivos = new List<ArchivoXmlDTO>(), message = "No hay archivos generados para este RNC" });

                var archivos = Directory.GetFiles(carpetaXmls, "*.xml")
                    .Select(path =>
                    {
                        bool puedeDescargarse = false;

                        try
                        {
                            string xmlContent = System.IO.File.ReadAllText(path);
                            var factura = XmlUtils.DeserializarXml<FacturasModels>(xmlContent);

                            // Obtener tipo eCF y monto total parseado
                            string tipoECF = factura?.Encabezado?.IdDoc?.TipoeCF ?? "";
                            string montoTotalStr = factura?.Encabezado?.Totales?.MontoTotal ?? "0";

                            decimal montoTotal = ParseDecimalFlexible(montoTotalStr);

                            // Condición para permitir descarga
                            puedeDescargarse = tipoECF == "32" && montoTotal < 250000;
                            _logger.LogInformation("Archivo: {Archivo}, TipoECF: {Tipo}, MontoTotalStr: {MontoStr}, MontoDecimal: {MontoDecimal}",
        Path.GetFileName(path), tipoECF, montoTotalStr, montoTotal);

                        }
                        catch
                        {
                            puedeDescargarse = false;
                            Console.WriteLine($"Error al procesar el archivo: {Path.GetFileName(path)}");
                        }

                        return new ArchivoXmlDTO
                        {
                            NombreArchivo = Path.GetFileName(path),
                            PuedeDescargarse = puedeDescargarse
                        };
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    rnc,
                    total = archivos.Count,
                    archivos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar archivos XML para RNC {RNC}", rnc);
                return StatusCode(500, new { success = false, message = "Error interno del servidor", detalle = ex.Message });
            }
        }

        public class ArchivoXmlDTO
        {
            public string NombreArchivo { get; set; }
            public bool PuedeDescargarse { get; set; }
        }


        /// <summary>
        /// Descarga un archivo XML previamente generado para un RNC específico,
        /// validando que sea de tipo e-CF 32 y que el monto total sea menor a 250,000.
        /// </summary>
        /// <param name="rnc">RNC del emisor del comprobante</param>
        /// <param name="fileName">Nombre del archivo XML a descargar</param>
        /// <returns>Archivo XML si cumple las condiciones, o error si no es válido o no existe</returns>

        [HttpGet("DescargarXml")]
        public IActionResult DescargarXml([FromQuery] string rnc, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(rnc) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest("RNC y nombre de archivo son requeridos");

            string folderPath = _fileStorageManager.GetDynamicFolderPath(rnc, FileStorageManager.StorageType.Certificacion);
            string filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Archivo no encontrado");

            try
            {
                string xmlContent = System.IO.File.ReadAllText(filePath);
                var invoice = XmlUtils.DeserializarXml<FacturasModels>(xmlContent);

                string ecfType = invoice?.Encabezado?.IdDoc?.TipoeCF ?? "";
                decimal totalAmount = ParseDecimalFlexible(invoice?.Encabezado?.Totales?.MontoTotal ?? "0");

                if (ecfType != "32" || totalAmount >= 250000)
                    return Forbid("No autorizado para descargar este archivo");

                return File(System.IO.File.ReadAllBytes(filePath), "application/xml", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar XML {FileName} para RNC {Rnc}", fileName, rnc);
                return StatusCode(500, "Error interno al procesar la descarga");
            }
        }

        [HttpGet("DescargarXmlGeneral")]
        public IActionResult DescargarXmlGeneral([FromQuery] string rnc, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(rnc) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest(new { success = false, message = "RNC y nombre de archivo son requeridos" });

            try
            {
                string folderPath = _fileStorageManager.GetDynamicFolderPath(rnc, FileStorageManager.StorageType.Certificacion);
                string filePath = Path.Combine(folderPath, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { success = false, message = "Archivo no encontrado" });

                return File(System.IO.File.ReadAllBytes(filePath), "application/xml", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar XML {FileName} para RNC {Rnc}", fileName, rnc);
                return StatusCode(500, new { success = false, message = "Error interno al procesar la descarga" });
            }
        }

        [HttpGet("VerContenidoXml")]
        public IActionResult VerContenidoXml([FromQuery] string rnc, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(rnc) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest(new { success = false, message = "RNC y nombre de archivo son requeridos" });

            try
            {
                string folderPath = _fileStorageManager.GetDynamicFolderPath(rnc, FileStorageManager.StorageType.Certificacion);
                string filePath = Path.Combine(folderPath, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { success = false, message = "Archivo no encontrado" });

                string content = System.IO.File.ReadAllText(filePath);
                return Ok(new { success = true, content });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer contenido XML {FileName} para RNC {Rnc}", fileName, rnc);
                return StatusCode(500, new { success = false, message = "Error interno al leer el archivo" });
            }
        }


        /// <summary>
        /// Lista los archivos XML de resúmenes generados para un RNC específico,
        /// ubicados en la carpeta Storage/Certificacion/Resumenes/{RNC}.
        /// Estos archivos pueden ser utilizados en el frontend para su posterior envío a la DGII.
        /// </summary>
        /// <param name="rnc">RNC del emisor para filtrar los resúmenes generados</param>
        /// <returns>Lista de archivos XML encontrados o mensaje indicando que no hay archivos disponibles</returns>

        [HttpGet("ListarResumenesGenerados")]
        public IActionResult ListarResumenesGenerados([FromQuery] string rnc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rnc))
                {
                    return BadRequest(new { success = false, message = "El RNC es requerido" });
                }

                // Obtener la ruta a la carpeta de resúmenes
                string carpetaResumenes = Path.Combine(
                    _env.ContentRootPath,
                    "Storage",
                    "Certificacion",
                    "Resumenes",
                    rnc
                );

                if (!Directory.Exists(carpetaResumenes))
                {
                    return Ok(new
                    {
                        success = true,
                        archivos = new string[0],
                        message = "No hay resúmenes generados para este RNC"
                    });
                }

                // Listar archivos XML de resúmenes
                var archivos = Directory.GetFiles(carpetaResumenes, "*.xml")
                    .Select(Path.GetFileName)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    rnc,
                    total = archivos.Count,
                    archivos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar resúmenes XML para RNC {RNC}", rnc);
                return StatusCode(500, new { success = false, message = "Error interno del servidor", detalle = ex.Message });
            }
        }


        /// <summary>
        /// Procesa un archivo Excel con facturas (ECF) y resúmenes (RFCE),
        /// generando los XML correspondientes firmados o con su código de seguridad.
        /// </summary>
        /// <param name="request">Archivo Excel y RNC de la empresa.</param>
        /// <returns>HTTP 200 con cantidad de XMLs generados o error en caso de fallo.</returns>
        [HttpPost("GenerarXmlsExcel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerarXmlsDesdeExcel([FromForm] GenerarXmlsExcelDTO request)
        {
            try
            {
                // Validaciones automáticas gracias a las data annotations
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Paso 1: Guardar archivo temporalmente
                var tempFilePath = await GuardarArchivoExcelTemporal(request.RNC, request.ExcelFile);

                // Paso 2: Procesar Excel y generar XMLs
                var resultado = await ProcesarExcelYGenerarXmls(request.RNC, tempFilePath);

                return Ok(new
                {
                    success = true,
                    generatedFiles = resultado.XmlsGenerados,
                    message = $"Se generaron {resultado.XmlsGenerados} XMLs correctamente",
                    path = _fileStorageManager.GetDynamicFolderPath(request.RNC, FileStorageManager.StorageType.Certificacion)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando XMLs para RNC: {RNC}", request.RNC);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error interno: {ex.Message}"
                });
            }
        }


        /// <summary>
        /// Envía un archivo XML a la DGII según su tipo y monto.
        /// </summary>
        /// <param name="rnc">El RNC asociado al archivo XML.</param>
        /// <param name="fileName">El nombre del archivo XML a enviar (con o sin extensión).</param>
        /// <returns>
        /// Retorna un IActionResult que indica el resultado del envío:
        /// - Si el archivo no existe, retorna NotFound.
        /// - Si el XML no tiene tipo o monto válido, retorna BadRequest.
        /// - Si el tipo es "32" y el monto es menor a 250,000, envía el XML como resumen RFCE.
        /// - En otro caso, envía el XML como factura (ECF).
        /// - En caso de error interno, retorna un StatusCode 500.
        /// </returns>

        [HttpPost("EnviarXmlSegunTipo")]
        public async Task<IActionResult> EnviarXmlSegunTipo([FromForm] string rnc, [FromForm] string fileName)
        {
            try
            {
                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    fileName += ".xml";

                // Rutas donde buscar el XML (facturas o resúmenes)
                var rutaFactura = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "XMLGenerados", rnc, rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml");
                var rutaResumen = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Resumenes", rnc, rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml");

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

        /// <summary>
        /// Envía un archivo XML de factura electrónica a la DGII y devuelve el estado de recepción.
        /// </summary>
        /// <param name="rnc">RNC de la empresa emisora.</param>
        /// <param name="fileName">Nombre del archivo XML a enviar.</param>
        /// <returns>
        /// Resultado HTTP con el estado de respuesta de la DGII o un error en caso de fallo.
        /// </returns>
        [HttpPost("EnviarfacturasDGII")]
        public async Task<IActionResult> EnviarXmlADgii([FromForm] string rnc, [FromForm] string fileName)
        {
            try
            {
                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    fileName += ".xml";

                var fileNameWithRnc = rnc + Path.GetFileNameWithoutExtension(fileName) + ".xml";
                var xmlPath = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "XMLGenerados", rnc, fileNameWithRnc);

                if (!System.IO.File.Exists(xmlPath))
                    return NotFound(new { success = false, message = "Archivo XML no encontrado." });

                var dgiiToken = await _semillaService.ObtenerTokenAsync(rnc);

                using var formContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(xmlPath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");
                formContent.Add(fileContent, "xml", fileNameWithRnc);

                // Use _httpClient (default, no internal auth handlers) for DGII requests
                using var dgiiRequest = new HttpRequestMessage(HttpMethod.Post, "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas");
                dgiiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dgiiToken);
                dgiiRequest.Content = formContent;

                var dgiiResponse = await _httpClient.SendAsync(dgiiRequest);
                if (!dgiiResponse.IsSuccessStatusCode)
                {
                    var errorBody = await dgiiResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Error al enviar XML a DGII: {Status} - {Content}", dgiiResponse.StatusCode, errorBody);
                    return StatusCode((int)dgiiResponse.StatusCode, new { success = false, message = errorBody });
                }

                var dgiiResponseContent = await dgiiResponse.Content.ReadAsStringAsync();
                var trackInfo = JsonSerializer.Deserialize<JsonElement>(dgiiResponseContent);

                if (!trackInfo.TryGetProperty("trackId", out var trackIdElement))
                    return StatusCode(502, new { success = false, message = "La respuesta de la DGII no contiene trackId." });

                var trackId = trackIdElement.GetString();
                await Task.Delay(5000);

                // Query DGII status endpoint directly using the same DGII token
                var statusDgiiUrl = $"{Constants.ConsultarFacturasEndpoint}?trackId={trackId}";
                using var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusDgiiUrl);
                statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dgiiToken);

                var statusResponse = await _httpClient.SendAsync(statusRequest);
                var statusBody = await statusResponse.Content.ReadAsStringAsync();

                if (statusResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Estatus DGII para trackId {TrackId}: {Body}", trackId, statusBody);
                    return Content(statusBody, "application/json");
                }

                _logger.LogWarning("Consulta de estatus DGII retornó {Status} para trackId {TrackId}: {Body}",
                    statusResponse.StatusCode, trackId, statusBody);

                return StatusCode((int)statusResponse.StatusCode, new { success = false, message = statusBody, trackId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar XML a la DGII para RNC {RNC}", rnc);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Envía un archivo XML de resumen de facturas electrónicas (RFCE) a la DGII y retorna directamente la respuesta recibida.
        /// </summary>
        /// <param name="rnc">RNC de la empresa emisora.</param>
        /// <param name="fileName">Nombre del archivo XML de resumen a enviar.</param>
        /// <returns>
        /// Respuesta directa de la DGII sobre el estado del resumen enviado.
        /// </returns>
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
                var xmlPath = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Resumenes", rnc, nombreArchivoCompleto);
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

        private async Task<string> GuardarArchivoExcelTemporal(string rnc, IFormFile archivoExcel)
        {
            try
            {
                // Usar FileStorageManager para guardar el archivo en la ubicación
                var nombreArchivo = $"{rnc}-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var rutaCompleta = await _fileStorageManager.SaveFileAsync(
                    rnc,
                    FileStorageManager.StorageType.PruebasExcel,
                    archivoExcel,
                    nombreArchivo);

                return rutaCompleta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar archivo Excel temporal para RNC: {RNC}", rnc);
                throw;
            }
        }

        private async Task<(int TotalFilas, int XmlsGenerados)> ProcesarExcelYGenerarXmls(string rnc, string rutaExcel)
        {
            int totalFacturas = 0;
            int totalResumenes = 0;
            int xmlsGenerados = 0;

            try
            {
                ExcelPackage.License.SetNonCommercialPersonal("eCertify");
                using var package = new ExcelPackage(new FileInfo(rutaExcel));

                // Procesar hoja ECF (Facturas)
                var ecfWorksheet = package.Workbook.Worksheets["ECF"] ??
                    throw new Exception("Hoja 'ECF' no encontrada");

                // Procesar hoja RFCE (Resúmenes)
                var rfceWorksheet = package.Workbook.Worksheets["RFCE"];
                if (rfceWorksheet == null)
                {
                    _logger.LogWarning("Hoja 'RFCE' no encontrada. No se generarán resúmenes.");
                }

                var empresa = await _empresaService.GetEmpresaByRncAsync(rnc);
                if (empresa == null)
                    throw new Exception($"Empresa con RNC {rnc} no encontrada");

                // 1. Procesar facturas (hoja ECF)
                var ecfHeader = ObtenerEncabezadosExcel(ecfWorksheet);
                totalFacturas = ecfWorksheet.Dimension.End.Row - 1;

                for (int row = 2; row <= ecfWorksheet.Dimension.End.Row; row++)
                {
                    try
                    {
                        var rowData = ObtenerDatosFila(ecfWorksheet, ecfHeader, row);
                        var factura = _facturasXmlService.MapearFacturaDesdeDiccionario(rowData);

                        if (factura != null && !string.IsNullOrWhiteSpace(factura.Encabezado?.IdDoc?.eNCF))
                        {
                            _logger.LogInformation("Procesando factura {Row} con NCF: {NCF}", row, factura.Encabezado.IdDoc.eNCF);

                            // Generar y guardar factura
                            var xmlFactura = await _facturasXmlService.GenerarXmlDesdeModeloAsync(factura);
                            var xmlDocFactura = new XmlDocument();
                            xmlDocFactura.LoadXml(xmlFactura);
                            var xmlFirmadoFactura = _semillaService.FirmarXml(xmlDocFactura, empresa, true);

                            await _fileStorageManager.SaveXmlAsync(
                                rnc,
                                xmlFirmadoFactura.OuterXml,
                                $"{rnc}{factura.Encabezado.IdDoc.eNCF}.xml",
                                FileStorageManager.StorageType.Certificacion);

                            xmlsGenerados++;
                        }
                    }
                    catch (Exception exFila)
                    {
                        _logger.LogError(exFila, "Error procesando fila de factura {Fila}", row);
                    }
                }

                // 2. Procesar resúmenes (hoja RFCE)
                if (rfceWorksheet != null)
                {
                    var rfceHeader = ObtenerEncabezadosExcel(rfceWorksheet);
                    totalResumenes = rfceWorksheet.Dimension.End.Row - 1;

                    for (int row = 2; row <= rfceWorksheet.Dimension.End.Row; row++)
                    {
                        try
                        {
                            var rowData = ObtenerDatosFila(rfceWorksheet, rfceHeader, row);
                            var resumen = _resumenesXmlService.MapearResumenDesdeDiccionario(rowData); // Necesitarás implementar este método

                            if (resumen != null && !string.IsNullOrWhiteSpace(resumen.Encabezado?.IdDoc?.eNCF))
                            {
                                _logger.LogInformation("Procesando resumen {Row} con NCF: {NCF}", row, resumen.Encabezado.IdDoc.eNCF);

                                // Generar y guardar resumen
                                var xmlResumen = await _resumenesXmlService.GenerarXmlDesdeModeloAsync(resumen);
                                var xmlDocResumen = new XmlDocument();
                                xmlDocResumen.LoadXml(xmlResumen);
                                //El resumen no lleva firma hasta que el CódigoSeguridadeCF sea extraido de su fatura principal
                                var xmlFirmadoResumen = xmlDocResumen;

                                //Se busca el nodo de firma para obtener el CódigoSeguridadeCF de la firma de la factura principal
                                var ecfPath = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "XMLGenerados", rnc, $"{rnc}{resumen.Encabezado.IdDoc.eNCF}.xml");
                                if (System.IO.File.Exists(ecfPath))
                                {
                                    var ecfDoc = new XmlDocument();
                                    ecfDoc.Load(ecfPath);

                                    var ecfNsManager = new XmlNamespaceManager(ecfDoc.NameTable);
                                    ecfNsManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                                    var signatureValueNode = ecfDoc.SelectSingleNode("//ds:SignatureValue", ecfNsManager);
                                    if (signatureValueNode != null)
                                    {
                                        var hash = signatureValueNode.InnerText;
                                        var codigoSeguridad = hash.Substring(0, 6);

                                        // Insertar CodigoSeguridadeCF en el resumen
                                        var encabezadoNode = xmlFirmadoResumen.SelectSingleNode("//Encabezado");
                                        if (encabezadoNode != null)
                                        {
                                            var nuevoNodo = xmlFirmadoResumen.CreateElement("CodigoSeguridadeCF");
                                            nuevoNodo.InnerText = codigoSeguridad;
                                            encabezadoNode.AppendChild(nuevoNodo);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No se encontró la factura firmada para extraer el CódigoSeguridadeCF: {Archivo}", ecfPath);
                                }

                                // Firmar el XML resumen (ya con CodigoSeguridadeCF)
                                var xmlFirmarResumen = _semillaService.FirmarXml(xmlDocResumen, empresa, false); // false: no es factura

                                // Insertar nodo Signature justo después de Encabezado
                                var rfceNode = xmlFirmarResumen.SelectSingleNode("/RFCE");
                                var encabezadoNodeFirmado = xmlFirmarResumen.SelectSingleNode("/RFCE/Encabezado");
                                var signatureNode = xmlFirmarResumen.SelectSingleNode("//Signature", new XmlNamespaceManager(xmlFirmadoResumen.NameTable));

                                if (rfceNode != null && encabezadoNodeFirmado != null && signatureNode != null)
                                {
                                    // Remover signatureNode de donde esté para luego insertarlo donde queremos
                                    signatureNode.ParentNode.RemoveChild(signatureNode);

                                    // Insertar signatureNode justo después de Encabezado
                                    rfceNode.InsertAfter(signatureNode, encabezadoNodeFirmado);
                                }

                                // Guardar archivo firmado con el Signature correctamente insertado

                                await _fileStorageManager.SaveResumenExcelAsync(
                                    rnc,
                                    xmlFirmadoResumen.OuterXml,
                                    $"{rnc}{resumen.Encabezado.IdDoc.eNCF}.xml");

                                xmlsGenerados++;
                            }
                        }
                        catch (Exception exFila)
                        {
                            _logger.LogError(exFila, "Error procesando fila de resumen {Fila}", row);
                        }
                    }
                }

                _logger.LogInformation($"Proceso completado. Facturas procesadas: {totalFacturas}, Resúmenes procesados: {totalResumenes}, XMLs generados: {xmlsGenerados}");
                return (totalFacturas + totalResumenes, xmlsGenerados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando Excel");
                throw;
            }
        }

        private Dictionary<string, int> ObtenerEncabezadosExcel(ExcelWorksheet worksheet)
        {
            var header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var colName = worksheet.Cells[1, col].Text.Trim();
                if (!string.IsNullOrEmpty(colName))
                    header[colName] = col;
            }

            if (header.Count == 0)
                throw new Exception("No se encontraron encabezados válidos en el archivo Excel");

            return header;
        }

        private Dictionary<string, string> ObtenerDatosFila(ExcelWorksheet worksheet, Dictionary<string, int> header, int row)
        {
            var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in header)
            {
                var val = worksheet.Cells[row, kvp.Value].Text?.Trim();
                if (!string.IsNullOrEmpty(val) && val != "#e")
                    rowData[kvp.Key] = val;
            }

            return rowData;
        }

        private async Task<XmlDocument> GenerarYFirmarXml(FacturasModels factura, string rnc)
        {
            try
            {
                // Obtener la empresa para firmar 
                var empresa = await _empresaService.GetEmpresaByRncAsync(rnc); 

                if (empresa == null)
                    throw new Exception($"No se encontró empresa con RNC {rnc}");

                // Generar XML inicial
                var xmlDoc = new XmlDocument();
                var xmlString = await _facturasXmlService.GenerarXmlDesdeModeloAsync(factura); 
                xmlDoc.LoadXml(xmlString);
                using (var reader = new StringReader(xmlString))
                {
                    using (var xmlReader = XmlReader.Create(reader))
                    {
                        xmlDoc.Load(xmlReader);
                    }
                }

                // Firmar el XML (con fecha)
                return _semillaService.FirmarXml(xmlDoc, empresa, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar o firmar XML");
                throw;
            }
        }

        private async Task GuardarXmlFirmado(XmlDocument xmlDoc, string rnc, FacturasModels factura)
        {
            try
            {
                var encf = factura.Encabezado?.IdDoc?.eNCF ?? "SINENC";
                var nombreArchivo = $"{rnc}{encf}.xml";

                // Guardar usando FileStorageManager
                await _fileStorageManager.SaveXmlAsync(
                    rnc,
                    xmlDoc.OuterXml,
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar XML firmado");
                throw;
            }
        }

        private decimal ParseDecimalFlexible(string input)
        {
            var culturas = new[]
            {
                CultureInfo.InvariantCulture,                    
                new CultureInfo("es-DO"),                        
                new CultureInfo("en-US"),                        
                new CultureInfo("fr-FR"),                        
                new CultureInfo("de-DE")
            };

            foreach (var cultura in culturas)
            {
                if (decimal.TryParse(input, NumberStyles.Any, cultura, out var result))
                    return result;
            }

            return 0;
        }


    }
}