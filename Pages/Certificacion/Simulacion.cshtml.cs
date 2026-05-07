using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Utils;
using eCertify.DTOs.Front;
using eCertify.Models;
using System.Text.Json;
using eCertify.Services.Front;

namespace eCertify.Pages.Certificacion
{
    public class SimulacionModel : PageModel
    {
        private readonly ILogger<SimulacionModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IPasosCompletadosService _pasosCompletadosService;
        private readonly PlanValidator _planValidator;

        public SimulacionModel(ILogger<SimulacionModel> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, IPasosCompletadosService pasosCompletadosService, PlanValidator planValidator)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _pasosCompletadosService = pasosCompletadosService;
            _planValidator = planValidator;
        }

        [BindProperty] public Simulacion simulacion { get; set; }
        [BindProperty(SupportsGet = false)] public string? JsonGenerado { get; set; }
        [TempData] public string JsonGeneradoTemp { get; set; }
        [TempData] public string[] MensajesLog { get; set; }
        [BindProperty] public bool MostrarModalDescarga { get; set; }
        [BindProperty] public string? ArchivoParaDescargar { get; set; }
        [BindProperty] public string? RncEmpresa { get; set; }

        public bool PasoCompletado { get; set; }

        public Empresa Empresa { get; set; }

        public async Task OnGetPasosAsync()
        {
            
            var pasos = await _pasosCompletadosService.ObtenerPasosAsync(User);

            // Buscar el paso actual (por nombre o ID)
            var paso = pasos.FirstOrDefault(p =>
                p.Nombre.Equals("Pruebas Simulación e-CF", StringComparison.OrdinalIgnoreCase));

            PasoCompletado = paso?.Completado ?? false;

        }

        public async Task<IActionResult> OnGetAsync()
        {
            var validacion = await _planValidator.VerificarPlanAsync();
            if (validacion != null)
                return validacion;

            await OnGetPasosAsync();

            Empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);

            // Recuperar datos de TempData si existen
            if (TempData.TryGetValue("JsonGenerado", out object json))
            {
                JsonGenerado = json.ToString();
                TempData.Keep("JsonGenerado");
            }

            if (!string.IsNullOrEmpty(JsonGeneradoTemp))
            {
                JsonGenerado = JsonGeneradoTemp;
                TempData.Keep(nameof(JsonGeneradoTemp));
            }

            // Inicializar MensajesLog si es null
            if (MensajesLog == null)
            {
                MensajesLog = Array.Empty<string>();
            }

            return Page();

        }

        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            const int pasoId = 4;
            const string pasoNombre = "Pruebas Simulación e-CF";

            try
            {
                var resultado = await PasosCompletadosHelper.RegistrarPasoCompletado(
                    _httpClientFactory,
                    _configuration,
                    User,
                    pasoNombre,
                    _logger,
                    pasoId);

                if (resultado)
                {
                    _logger.LogInformation("Paso {PasoId} ({PasoNombre}) registrado para empresa {EmpresaId}",
                        pasoId, pasoNombre, ClaimHelper.ObtenerEmpresaDesdeClaims(User).ID);

                    // Devolver JSON en lugar de recargar la página
                    return new JsonResult(new { success = true, message = "¡Paso completado correctamente!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No se pudo registrar el paso completado" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al marcar paso como completado");
                return new JsonResult(new { success = false, message = "Ocurrió un error inesperado" });
            }
        }

        public IActionResult OnPostGenerarJson()
        {
            try
            {
                Empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);
                var mensajesLog = MensajesLog?.ToList() ?? new List<string>();
                mensajesLog.Clear();
                // Validar modelo
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Modelo inválido al generar JSON.");
                    foreach (var entry in ModelState)
                    {
                        foreach (var error in entry.Value.Errors)
                        {
                            _logger.LogWarning("Campo inválido: {Campo} - Error: {Error}", entry.Key, error.ErrorMessage);
                        }
                    }
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error: Datos del formulario inválidos");
                    MensajesLog = mensajesLog.ToArray();
                    TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                    return Page();
                }

                // Validaciones adicionales
                if (string.IsNullOrEmpty(simulacion.TipoECF))
                {
                    _logger.LogWarning("TipoECF no especificado.");
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error: Debe seleccionar un tipo de ECF");
                    MensajesLog = mensajesLog.ToArray();
                    TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                    return Page();
                }

                if (simulacion.SecuenciaECF <= 0)
                {
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error: La secuencia debe ser un número positivo");
                    MensajesLog = mensajesLog.ToArray();
                    TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                    return Page();
                }

                // Generar JSON con soporte para caracteres especiales (acentos, ñ, etc.)
                var request = CrearFactura();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                JsonGenerado = JsonSerializer.Serialize(request, options);

                JsonGeneradoTemp = JsonGenerado;

                _logger.LogInformation("JSON generado correctamente para RNC {RNC}", Empresa?.RNC);
                mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ✅ JSON generado correctamente");

                MensajesLog = mensajesLog.ToArray();
                TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                TempData["JsonGenerado"] = JsonGenerado;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el JSON.");
                var mensajesLog = MensajesLog?.ToList() ?? new List<string>();
                mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error al generar JSON: {ex.Message}");
                MensajesLog = mensajesLog.ToArray();
                TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                return Page();
            }
        }


        public async Task<IActionResult> OnPostEnviarFactura()
        {
            try
            {
                _logger.LogInformation("Iniciando envío de factura...");
                
                Empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);
                var mensajesLog = MensajesLog?.ToList() ?? new List<string>();
                mensajesLog.Clear();

                if (string.IsNullOrWhiteSpace(JsonGenerado))
                {
                    _logger.LogWarning("No hay JSON generado para enviar.");
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ No hay JSON generado para enviar.");
                    MensajesLog = mensajesLog.ToArray();
                    TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                    return Page();
                }

                var clientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient("ApiClient");

                // Paso 1: Generar el archivo XML
                var content = new StringContent(JsonGenerado, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/Simulacion/GenerarFacturasDesdeJson", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Error al generar XML. StatusCode: {StatusCode}, Detalle: {Error}", response.StatusCode, error);
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error al generar XML: {response.StatusCode}");
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❗ Detalle: {error}");

                    MensajesLog = mensajesLog.ToArray();

                    TempData["MensajesLog"] = JsonSerializer.Serialize(mensajesLog);
                    TempData["JsonGenerado"] = JsonGenerado;
                    return Page();
                }
                mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ✅ XML generado correctamente.");

                // Paso 2: Extraer el RNC y el eNCF del JSON para hacer el envío
                using var jsonDoc = JsonDocument.Parse(JsonGenerado);
                var root = jsonDoc.RootElement;

                string rnc = root.GetProperty("ECF")
                 .GetProperty("Encabezado")
                 .GetProperty("Emisor")
                 .GetProperty("RNCEmisor")
                 .GetString();

                string fileName = root.GetProperty("ECF")
                                      .GetProperty("Encabezado")
                                      .GetProperty("IdDoc")
                                      .GetProperty("eNCF")
                                      .GetString();

                string tipoECF = root.GetProperty("ECF")
                           .GetProperty("Encabezado")
                           .GetProperty("IdDoc")
                           .GetProperty("TipoeCF")
                           .GetString();

                string montoTotalStr = root.GetProperty("ECF")
                                          .GetProperty("Encabezado")
                                          .GetProperty("Totales")
                                          .GetProperty("MontoTotal")
                                          .GetString();

                if (tipoECF == "32" && decimal.TryParse(montoTotalStr, out decimal montoTotal) && montoTotal < 250000)
                {
                    ArchivoParaDescargar = fileName + ".xml";
                    RncEmpresa = rnc;
                    MostrarModalDescarga = true;

                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ Esta factura de consumo es menor a RD$250,000. Se mostrará opción de descarga manual.");
                    MensajesLog = mensajesLog.ToArray();

                    TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                    TempData["JsonGenerado"] = JsonGenerado;

                    
                }
                var formData = new Dictionary<string, string>
                {
                    { "rnc", rnc },
                    { "fileName", fileName }
                };
                var contentEnvio = new FormUrlEncodedContent(formData);

                var envioResponse = await client.PostAsync("api/Simulacion/EnviarXmlSegunTipo", contentEnvio);

                if (envioResponse.IsSuccessStatusCode)
                {
                    var envioContent = await envioResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Factura enviada correctamente. Respuesta: {Respuesta}", envioContent);
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Factura enviada correctamente.");
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] 🔁 Respuesta: {envioContent}");
                }
                else
                {
                    var errorEnvio = await envioResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Error al enviar factura. StatusCode: {StatusCode}, Detalle: {Error}", envioResponse.StatusCode, errorEnvio);
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error al enviar factura: {envioResponse.StatusCode}");
                    mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❗ Detalle: {errorEnvio}");
                }


                MensajesLog = mensajesLog.ToArray();
                TempData["JsonGenerado"] = JsonGenerado;
                TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción durante el envío de la factura.");
                var mensajesLog = MensajesLog?.ToList() ?? new List<string>();
                mensajesLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Excepción: {ex.Message}");

                MensajesLog = mensajesLog.ToArray();
                TempData["MensajesLog"] = JsonSerializer.Serialize(MensajesLog);
                TempData["JsonGenerado"] = JsonGenerado;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDescargarXml()
        {
            if (string.IsNullOrWhiteSpace(ArchivoParaDescargar) || string.IsNullOrWhiteSpace(RncEmpresa))
                return BadRequest("Parámetros inválidos para descarga");

            var clientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("ApiClient");

            var requestUri = $"api/Simulacion/DescargarXml?rnc={RncEmpresa}&fileName={Path.GetFileNameWithoutExtension(ArchivoParaDescargar)}";
            var response = await client.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Error al descargar el archivo XML");

            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            var nombreDescarga = $"{RncEmpresa}{Path.GetFileNameWithoutExtension(ArchivoParaDescargar)}.xml";
            return File(contentBytes, "application/xml", nombreDescarga);
        }

        public IActionResult OnPostCerrarModal()
        {
            MostrarModalDescarga = false; 
            return Page();
        }


        private SimulacionECFRequestDTO CrearFactura()
        {
            var baseMonto = simulacion.ProductoCantidad * simulacion.ProductoPrecio;
            var montoItbis = simulacion.ProductoItbis > 0 ? (baseMonto * simulacion.ProductoItbis.Value / 100) : 0;
            var montoTotal = baseMonto + montoItbis;

            var request = new SimulacionECFRequestDTO
            {
                Ecf = new SimulacionECFRequestDTO.ECF
                {
                    Encabezado = new SimulacionECFRequestDTO.Encabezado
                    {
                        Version = "1.0",
                        IdDoc = new SimulacionECFRequestDTO.IdDoc
                        {
                            TipoeCF = simulacion.TipoECF,
                            eNCF = GenerarSecuencia(),
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TablaFormasPago = new List<SimulacionECFRequestDTO.FormaDePago>
                            {
                                new() {
                                    FormaPago = "1",
                                    MontoPago = montoTotal.ToString("F2")
                                }
                            }
                        },
                        Emisor = new SimulacionECFRequestDTO.Emisor
                        {
                            RNCEmisor = Empresa.RNC,
                            RazonSocialEmisor = Empresa.RazonSocial,
                            NombreComercial = Empresa.RazonSocial,
                            DireccionEmisor = Empresa.Direccion,
                            CorreoEmisor = Empresa.Email,
                            FechaEmision = DateTime.Now.ToString("dd-MM-yyyy")
                        },
                        Comprador = new SimulacionECFRequestDTO.Comprador
                        {
                            RNCComprador = simulacion.ClienteRNC,
                            RazonSocialComprador = simulacion.ClienteNombre
                        },
                        Totales = new SimulacionECFRequestDTO.Totales()
                    },
                    DetallesItems = new List<SimulacionECFRequestDTO.Item>
                    {
                        new()
                        {
                            NumeroLinea = "1",
                            IndicadorFacturacion = CalcularIndicadorFacturacion(simulacion.ProductoItbis),
                            NombreItem = simulacion.ProductoNombre,
                            IndicadorBienoServicio = simulacion.ProductoTipo == "B" ? "1" : "2",
                            CantidadItem = simulacion.ProductoCantidad.ToString("F2"),
                            UnidadMedida = simulacion.UnidadeMedida,
                            PrecioUnitarioItem = simulacion.ProductoPrecio.ToString("F2"),
                            MontoItem = baseMonto.ToString("F2")
                        }
                    }
                }
            };

            // Configuraciones específicas por tipo de ECF
            switch (simulacion.TipoECF)
            {
                case "31": // Factura de crédito fiscal sin ITBIS o con ITBIS
                    if (simulacion.ProductoItbis > 0)
                    {
                        request.Ecf.Encabezado.IdDoc.indicadorMontoGravado = "0";
                        request.Ecf.Encabezado.Totales.montoGravadoTotal = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.montoGravadoI1 = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.itbiS1 = simulacion.ProductoItbis.Value.ToString("0");
                        request.Ecf.Encabezado.Totales.totalITBIS = montoItbis.ToString("F2");
                        request.Ecf.Encabezado.Totales.totalITBIS1 = montoItbis.ToString("F2");
                        request.Ecf.Encabezado.Totales.MontoTotal = montoTotal.ToString("F2");
                    }
                    else
                    {
                        request.Ecf.Encabezado.Totales.MontoExento = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");
                    }
                    break;

                case "32": // Factura de consumo con ITBIS
                    
                    if (simulacion.ProductoItbis > 0)
                    {
                        request.Ecf.Encabezado.IdDoc.indicadorMontoGravado = "0";
                        request.Ecf.Encabezado.Totales.montoGravadoTotal = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.montoGravadoI1 = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.itbiS1 = simulacion.ProductoItbis.Value.ToString("0");
                        request.Ecf.Encabezado.Totales.totalITBIS = montoItbis.ToString("F2");
                        request.Ecf.Encabezado.Totales.totalITBIS1 = montoItbis.ToString("F2");
                        request.Ecf.Encabezado.Totales.MontoTotal = montoTotal.ToString("F2");
                        request.Ecf.Encabezado.IdDoc.TablaFormasPago = null;
                    }
                    else
                    {
                        request.Ecf.Encabezado.Totales.MontoExento = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.IdDoc.TablaFormasPago = null;
                    }
                    
                    // Para montos mayores a 250,000 se incluye TablaFormasPago
                    if (montoTotal > 250000)
                    {
                        request.Ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = null;
                        request.Ecf.Encabezado.IdDoc.TablaFormasPago = new List<SimulacionECFRequestDTO.FormaDePago>
                        {
                            new() {
                                FormaPago = "1",
                                MontoPago = montoTotal.ToString("F2")
                            }
                        };
                    }
                    break;

                case "33": // Nota de débito que modifica E31
                    if (simulacion.ProductoItbis > 0)
                    {
                        request.Ecf.Encabezado.IdDoc.indicadorMontoGravado = "0";
                        request.Ecf.Encabezado.Totales.montoGravadoTotal = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.montoGravadoI1 = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.itbiS1 = simulacion.ProductoItbis.Value.ToString("0");
                        request.Ecf.Encabezado.Totales.totalITBIS = montoItbis.ToString("F2");
                        request.Ecf.Encabezado.Totales.totalITBIS1 = montoItbis.ToString("F2");
                        request.Ecf.Encabezado.Totales.MontoTotal = montoTotal.ToString("F2");
                    }
                    else
                    {
                        request.Ecf.Encabezado.Totales.MontoExento = baseMonto.ToString("F2");
                        request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");
                    }

                    request.Ecf.InformacionReferencia = new SimulacionECFRequestDTO.InformacionReferencia
                    {
                        NCFModificado = simulacion.NCFModificado,
                        FechaNCFModificado = DateTime.Now.ToString("dd-MM-yyyy"),
                        CodigoModificacion = "3", // 3 = Corrección de datos
                        RazonModificacion = simulacion.RazonModificacion
                    };
                    break;


                case "34": // Nota de crédito que modifica E31
                    request.Ecf.Encabezado.IdDoc.indicadorNotaCredito = "0";

                    if (simulacion.ProductoItbis > 0)
                    {
                        request.Ecf.Encabezado.IdDoc.indicadorMontoGravado = "0";
                        request.Ecf.Encabezado.Totales.montoGravadoTotal = "0.00";
                        request.Ecf.Encabezado.Totales.montoGravadoI1 = "0.00";
                        request.Ecf.Encabezado.Totales.itbiS1 = simulacion.ProductoItbis.Value.ToString("0");
                        request.Ecf.Encabezado.Totales.totalITBIS = "0.00";
                        request.Ecf.Encabezado.Totales.totalITBIS1 = "0.00";
                        request.Ecf.Encabezado.Totales.MontoTotal = "0.00";
                    }
                    else
                    {
                        request.Ecf.Encabezado.Totales.MontoExento = "0.00";
                        request.Ecf.Encabezado.Totales.MontoTotal = "0.00";
                    }

                    // En los detalles, el monto debe ser 0
                    request.Ecf.DetallesItems[0].PrecioUnitarioItem = "0.00";
                    request.Ecf.DetallesItems[0].MontoItem = "0.00";

                    request.Ecf.InformacionReferencia = new SimulacionECFRequestDTO.InformacionReferencia
                    {
                        NCFModificado = simulacion.NCFModificado,
                        FechaNCFModificado = DateTime.Now.ToString("dd-MM-yyyy"),
                        CodigoModificacion = "2", // 2 = Error en datos
                        RazonModificacion = simulacion.RazonModificacion
                    };
                    request.Ecf.Encabezado.IdDoc.TablaFormasPago = null;
                    request.Ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = null;

                    break;

                case "41": // Comprobante de compra para proveedores informales
                    request.Ecf.Encabezado.IdDoc.indicadorMontoGravado = "0";
                    request.Ecf.Encabezado.Totales.montoGravadoTotal = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.montoGravadoI1 = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.itbiS1 = simulacion.ProductoItbis.Value.ToString("0");
                    request.Ecf.Encabezado.Totales.totalITBIS = montoItbis.ToString("F2");
                    request.Ecf.Encabezado.Totales.totalITBIS1 = montoItbis.ToString("F2");
                    request.Ecf.Encabezado.Totales.MontoTotal = montoTotal.ToString("F2");
                    request.Ecf.Encabezado.Totales.valorPagar = montoTotal.ToString("F2");
                    request.Ecf.Encabezado.Totales.totalITBISRetenido = montoItbis.ToString("F2");
                    request.Ecf.Encabezado.Totales.totalISRRetencion = (baseMonto * 0.10m).ToString("F2");

                    // Agregar retenciones al detalle
                    request.Ecf.DetallesItems[0].retencion = new SimulacionECFRequestDTO.Retencion
                    {
                        indicadorAgenteRetencionoPercepcion = "1",
                        montoITBISRetenido = montoItbis.ToString("F2"),
                        montoISRRetenido = (baseMonto * 0.10m).ToString("F2")
                    };
                    request.Ecf.Encabezado.IdDoc.TipoIngresos = null;
                    break;

                case "43": // Gastos menores
                    request.Ecf.Encabezado.Totales.MontoExento = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Comprador = null; // No tiene comprador
                    request.Ecf.Encabezado.IdDoc.TipoIngresos = null;
                    request.Ecf.Encabezado.IdDoc.TipoPago = null;
                    request.Ecf.Encabezado.IdDoc.TablaFormasPago = null;
                    foreach (var item in request.Ecf.DetallesItems)
                    {
                        item.IndicadorFacturacion = "4"; // Exento
                    }

                    break;

                case "44": // Regímenes especiales de tributación
                    request.Ecf.Encabezado.Totales.MontoExento = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.valorPagar = baseMonto.ToString("F2");
                    foreach (var item in request.Ecf.DetallesItems)
                    {
                        item.IndicadorFacturacion = "4"; // Exento
                    }
                    break;

                case "45": // Gubernamental
                    request.Ecf.Encabezado.IdDoc.indicadorMontoGravado = "0";
                    request.Ecf.Encabezado.Totales.montoGravadoTotal = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.montoGravadoI1 = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.itbiS1 = simulacion.ProductoItbis.Value.ToString("0");
                    request.Ecf.Encabezado.Totales.totalITBIS = montoItbis.ToString("F2");
                    request.Ecf.Encabezado.Totales.totalITBIS1 = montoItbis.ToString("F2");
                    request.Ecf.Encabezado.Totales.MontoTotal = montoTotal.ToString("F2");
                    break;

                case "46": // Comprobante para exportaciones
                    request.Ecf.Encabezado.Totales.montoGravadoTotal = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.montoGravadoI3 = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.itbiS3 = "0";
                    request.Ecf.Encabezado.Totales.totalITBIS = "0.00";
                    request.Ecf.Encabezado.Totales.totalITBIS3 = "0.00";
                    request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");

                    request.Ecf.Encabezado.IdDoc.TablaFormasPago = new List<SimulacionECFRequestDTO.FormaDePago>
                    {
                        new()
                        {
                            FormaPago = "1",
                            MontoPago = baseMonto.ToString("F2")
                        }
                    };

                    request.Ecf.DetallesItems[0].IndicadorFacturacion = "3";
                    break;

                case "47": // Comprobante para pagos al exterior
                    request.Ecf.Encabezado.IdDoc.numeroCuentaPago = "BB00058745214789635111111111";
                    request.Ecf.Encabezado.IdDoc.bancoPago = "BB0111111111111111111111111111111111111111111111111111111111111111111111111";
                    request.Ecf.Encabezado.Comprador.identificadorExtranjero = simulacion.ClienteRNC;
                    request.Ecf.Encabezado.Comprador.RNCComprador = null;
                    request.Ecf.Encabezado.Totales.MontoExento = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.MontoTotal = baseMonto.ToString("F2");
                    request.Ecf.Encabezado.Totales.totalISRRetencion = (simulacion.RetencionISR ?? baseMonto * 0.10m).ToString("F2");
                    request.Ecf.Encabezado.IdDoc.TipoIngresos = null;
                    request.Ecf.Encabezado.IdDoc.TipoPago = null;
                    request.Ecf.Encabezado.IdDoc.TablaFormasPago = null;

                    foreach (var item in request.Ecf.DetallesItems)
                    {
                        item.IndicadorFacturacion = "4"; // Exento
                    }

                    // Agregar retención ISR al detalle
                    request.Ecf.DetallesItems[0].retencion = new SimulacionECFRequestDTO.Retencion
                    {
                        indicadorAgenteRetencionoPercepcion = "1",
                        montoISRRetenido = (simulacion.RetencionISR ?? baseMonto * 0.10m).ToString("F2")
                    };
                    break;

                default:
                    throw new ArgumentException($"Tipo de ECF no soportado: {simulacion.TipoECF}");
            }

            return request;
        }

        string CalcularIndicadorFacturacion(decimal? itbis)
        {
            if (itbis == 18) return "1";
            if (itbis == 16) return "2";
            if (itbis == 0) return "3";
            return "4";
        }


        private string GenerarSecuencia()
        {
            return $"E{simulacion.TipoECF}{simulacion.SecuenciaECF.ToString().PadLeft(10, '0')}";
        }
    }
}