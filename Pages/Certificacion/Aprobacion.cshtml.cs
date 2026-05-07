using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using OfficeOpenXml;
using Newtonsoft.Json;
using eCertify.DTOs.Front; 
using eCertify.Utils;
using eCertify.Services.Front;

namespace eCertify.Pages.Certificacion
{
    public class AprobacionModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AprobacionModel> _logger;
        private readonly IPasosCompletadosService _pasosCompletadosService;
        private readonly PlanValidator _planValidator;

        public AprobacionModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AprobacionModel> logger, IPasosCompletadosService pasosCompletadosService, PlanValidator planValidator)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _pasosCompletadosService = pasosCompletadosService;
            _planValidator = planValidator;
        }


        [BindProperty]
        public IFormFile? ExcelFile { get; set; }

        public List<AprobacionDTO> FilasProcesadas { get; set; } = new List<AprobacionDTO>();

        public bool PasoCompletado { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var validacion = await _planValidator.VerificarPlanAsync();
            if(validacion != null)
                return validacion;

            var pasos = await _pasosCompletadosService.ObtenerPasosAsync(User);

            // Buscar el paso actual (por nombre o ID)
            var paso = pasos.FirstOrDefault(p =>
                p.Nombre.Equals("Aprobaci�n Comercial", StringComparison.OrdinalIgnoreCase));

            PasoCompletado = paso?.Completado ?? false;

            return Page();
        }

        public async Task<IActionResult> OnPostCargarExcelAsync()
        {
            try
            {
                var file = Request.Form.Files["ExcelFile"];

                if (file == null || file.Length == 0)
                    return BadRequest("No se ha seleccionado ning�n archivo.");

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                    return BadRequest("Solo se permiten archivos Excel (.xlsx, .xls).");

                var filas = new List<AprobacionDTO>();
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    using (var package = new ExcelPackage(stream))
                    {
                        if (package.Workbook.Worksheets.Count == 0)
                            return BadRequest("El archivo no contiene hojas de c�lculo.");

                        var worksheet = package.Workbook.Worksheets[0];

                        if (worksheet.Dimension == null)
                            return BadRequest("La hoja de c�lculo est� vac�a.");

                        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                        {
                            filas.Add(new AprobacionDTO
                            {
                                NumeroFila = row - 1,
                                Version = worksheet.Cells[row, 1]?.Text?.Trim(),
                                RncEmisor = worksheet.Cells[row, 2]?.Text?.Trim(),
                                ENCf = worksheet.Cells[row, 3]?.Text?.Trim(),
                                FechaEmision = worksheet.Cells[row, 4]?.Text?.Trim(),
                                MontoTotal = decimal.TryParse(worksheet.Cells[row, 5]?.Text, out var m) ? m : 0,
                                RncComprador = worksheet.Cells[row, 6]?.Text?.Trim(),
                                Estado = int.TryParse(worksheet.Cells[row, 7]?.Text, out var est) ? est : 0,
                                DetalleMotivoRechazo = worksheet.Cells[row, 8]?.Text?.Trim(),
                                FechaHoraAprobacionComercial = worksheet.Cells[row, 9]?.Text?.Trim()
                            });
                        }
                    }
                }

                return new JsonResult(filas);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en OnPostCargarExcelAsync: {ex.Message}");
                return BadRequest($"Error al procesar el archivo: {ex.Message}");
            }
        }



        // CAMBIO AQU�: Usa AprobacionDTO directamente
        public async Task<IActionResult> OnPostEnviarFilaAsync([FromBody] AprobacionDTO fila)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // Construir din�micamente el detalle solo con campos que tienen valor
                var detalleAprobacionComercial = new Dictionary<string, object?>();

                if (!string.IsNullOrWhiteSpace(fila.Version))
                    detalleAprobacionComercial["version"] = fila.Version;

                if (!string.IsNullOrWhiteSpace(fila.RncEmisor))
                    detalleAprobacionComercial["rncEmisor"] = fila.RncEmisor;

                if (!string.IsNullOrWhiteSpace(fila.ENCf))
                    detalleAprobacionComercial["eNCF"] = fila.ENCf;

                if (fila.FechaEmision != default)
                    detalleAprobacionComercial["fechaEmision"] = fila.FechaEmision;

                if (fila.MontoTotal > 0)
                    detalleAprobacionComercial["montoTotal"] = fila.MontoTotal;

                if (!string.IsNullOrWhiteSpace(fila.RncComprador))
                    detalleAprobacionComercial["rncComprador"] = fila.RncComprador;

                if (fila.Estado != 0)
                    detalleAprobacionComercial["estado"] = fila.Estado;

                if (!string.IsNullOrWhiteSpace(fila.DetalleMotivoRechazo))
                    detalleAprobacionComercial["detalleMotivoRechazo"] = fila.DetalleMotivoRechazo;

                if (fila.FechaHoraAprobacionComercial != default)
                    detalleAprobacionComercial["fechaHoraAprobacionComercial"] = fila.FechaHoraAprobacionComercial;

                var requestBody = new
                {
                    detalleAprobacionComercial = detalleAprobacionComercial
                };

                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody, jsonSettings), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("AprobacionComercial/GenerarXmlACECF", content);
                var responseText = await response.Content.ReadAsStringAsync();

                int estadoRespuesta = 0;
                dynamic responseJson = null;

                if (!string.IsNullOrEmpty(responseText))
                {
                    responseJson = JsonConvert.DeserializeObject<dynamic>(responseText);
                    estadoRespuesta = responseJson?.estado ?? 0;
                }

                return new JsonResult(new
                {
                    success = response.IsSuccessStatusCode,
                    estado = estadoRespuesta,
                    mensaje = responseJson?.mensaje ?? "Sin mensaje",
                    detalles = responseJson?.detalles ?? new List<string>(),
                    numeroFila = fila.NumeroFila
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en OnPostEnviarFilaAsync: {ex.Message}");
                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    numeroFila = fila.NumeroFila
                });
            }
        }

        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            const int pasoId = 3;
            const string pasoNombre = "Aprobaci�n Comercial";

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

                    // Devolver JSON en lugar de recargar la p�gina
                    return new JsonResult(new { success = true, message = "�Paso completado correctamente!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No se pudo registrar el paso completado" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cr�tico al marcar paso como completado");
                return new JsonResult(new { success = false, message = "Ocurri� un error inesperado" });
            }
        }

    }
}
