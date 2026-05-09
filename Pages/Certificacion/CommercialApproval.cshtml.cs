using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using eCertify.DTOs.Front;
using eCertify.Services.Front;
using eCertify.Utils;
using System.Text;
using Newtonsoft.Json;

namespace eCertify.Pages.Certificacion
{
    public class CommercialApprovalModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CommercialApprovalModel> _logger;
        private readonly IPasosCompletadosService _stepCompletionService;
        private readonly PlanValidator _planValidator;

        public CommercialApprovalModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CommercialApprovalModel> logger,
            IPasosCompletadosService stepCompletionService,
            PlanValidator planValidator)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _stepCompletionService = stepCompletionService;
            _planValidator = planValidator;
        }

        public bool StepCompleted { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var planCheck = await _planValidator.VerificarPlanAsync();
            if (planCheck != null)
                return planCheck;

            var steps = await _stepCompletionService.ObtenerPasosAsync(User);
            var step = steps.FirstOrDefault(s =>
                s.Name.Equals("Aprobación Comercial", StringComparison.OrdinalIgnoreCase));

            StepCompleted = step?.Completed ?? false;
            return Page();
        }

        public async Task<IActionResult> OnPostLoadExcelAsync()
        {
            try
            {
                var file = Request.Form.Files["ExcelFile"];

                if (file == null || file.Length == 0)
                    return BadRequest("No se ha seleccionado ningún archivo.");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                    return BadRequest("Solo se permiten archivos Excel (.xlsx, .xls).");

                var rows = new List<CommercialApprovalRowDto>();

                ExcelPackage.License.SetNonCommercialPersonal("eCertify");
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var package = new ExcelPackage(stream);

                if (package.Workbook.Worksheets.Count == 0)
                    return BadRequest("El archivo no contiene hojas de cálculo.");

                var sheet = package.Workbook.Worksheets[0];

                if (sheet.Dimension == null)
                    return BadRequest("La hoja de cálculo está vacía.");

                for (int row = 2; row <= sheet.Dimension.End.Row; row++)
                {
                    rows.Add(new CommercialApprovalRowDto
                    {
                        RowNumber                 = row - 1,
                        Version                   = sheet.Cells[row, 1]?.Text?.Trim(),
                        IssuerRnc                 = sheet.Cells[row, 2]?.Text?.Trim(),
                        Encf                      = sheet.Cells[row, 3]?.Text?.Trim(),
                        IssueDate                 = sheet.Cells[row, 4]?.Text?.Trim(),
                        TotalAmount               = decimal.TryParse(sheet.Cells[row, 5]?.Text, out var amt) ? amt : 0,
                        BuyerRnc                  = sheet.Cells[row, 6]?.Text?.Trim(),
                        Status                    = int.TryParse(sheet.Cells[row, 7]?.Text, out var st) ? st : 0,
                        RejectionDetail           = sheet.Cells[row, 8]?.Text?.Trim(),
                        CommercialApprovalDateTime = sheet.Cells[row, 9]?.Text?.Trim()
                    });
                }

                return new JsonResult(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing commercial approval Excel file");
                return BadRequest("Error al procesar el archivo.");
            }
        }

        public async Task<IActionResult> OnPostSendRowAsync([FromBody] CommercialApprovalRowDto row)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new Dictionary<string, object?>();

                if (!string.IsNullOrWhiteSpace(row.Version))
                    payload["version"] = row.Version;
                if (!string.IsNullOrWhiteSpace(row.IssuerRnc))
                    payload["rncEmisor"] = row.IssuerRnc;
                if (!string.IsNullOrWhiteSpace(row.Encf))
                    payload["eNCF"] = row.Encf;
                if (!string.IsNullOrWhiteSpace(row.IssueDate))
                    payload["fechaEmision"] = row.IssueDate;
                if (row.TotalAmount > 0)
                    payload["montoTotal"] = row.TotalAmount;
                if (!string.IsNullOrWhiteSpace(row.BuyerRnc))
                    payload["rncComprador"] = row.BuyerRnc;
                if (row.Status != 0)
                    payload["estado"] = row.Status;
                if (!string.IsNullOrWhiteSpace(row.RejectionDetail))
                    payload["detalleMotivoRechazo"] = row.RejectionDetail;
                if (!string.IsNullOrWhiteSpace(row.CommercialApprovalDateTime))
                    payload["fechaHoraAprobacionComercial"] = row.CommercialApprovalDateTime;

                var requestBody = new { detalleAprobacionComercial = payload };
                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody, jsonSettings),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync("CommercialApproval/GenerateApprovalXml", content);
                var responseText = await response.Content.ReadAsStringAsync();

                int dgiiStatus = 0;
                dynamic? responseJson = null;

                if (!string.IsNullOrEmpty(responseText))
                {
                    responseJson = JsonConvert.DeserializeObject<dynamic>(responseText);
                    dgiiStatus = (int)(responseJson?.estado ?? 0);
                }

                return new JsonResult(new
                {
                    success  = response.IsSuccessStatusCode,
                    estado   = dgiiStatus,
                    mensaje  = (string?)(responseJson?.mensaje) ?? "Sin mensaje",
                    detalles = (object?)(responseJson?.detalles) ?? new List<string>(),
                    rowNumber = row.RowNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending commercial approval row {RowNumber}", row.RowNumber);
                return new JsonResult(new
                {
                    success   = false,
                    mensaje   = "Error interno al enviar la aprobación.",
                    rowNumber = row.RowNumber
                });
            }
        }

        public async Task<IActionResult> OnPostMarkCompletedAsync()
        {
            const int stepId = 3;
            const string stepName = "Aprobación Comercial";

            try
            {
                var success = await PasosCompletadosHelper.RegistrarPasoCompletado(
                    _httpClientFactory,
                    _configuration,
                    User,
                    stepName,
                    _logger,
                    stepId);

                if (success)
                {
                    _logger.LogInformation("Step {StepId} ({StepName}) marked as completed", stepId, stepName);
                    return new JsonResult(new { success = true, message = "¡Paso completado correctamente!" });
                }

                return new JsonResult(new { success = false, message = "No se pudo registrar el paso completado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking commercial approval step as completed");
                return new JsonResult(new { success = false, message = "Ocurrió un error inesperado." });
            }
        }
    }
}
