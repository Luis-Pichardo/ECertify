using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Services.Front;
using eCertify.Utils;
using System.Net.Http.Headers;

namespace eCertify.Pages.Certificacion
{
    public class SignDocumentsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SignDocumentsModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPasosCompletadosService _stepTrackingService;
        private readonly PlanValidator _planValidator;

        private const string ctxPostulation = "postulacion";
        private const string ctxDeclaration = "declaracion";

        public SignDocumentsModel(
            IHttpClientFactory httpClientFactory,
            ILogger<SignDocumentsModel> logger,
            IConfiguration configuration,
            IPasosCompletadosService pasosCompletadosService,
            PlanValidator planValidator)
        {
            _httpClientFactory   = httpClientFactory;
            _logger              = logger;
            _configuration       = configuration;
            _stepTrackingService = pasosCompletadosService;
            _planValidator       = planValidator;
        }

        [BindProperty]
        public IFormFile? xmlFile { get; set; }

        [BindProperty(SupportsGet = true)]
        public string documentType { get; set; } = string.Empty;

        public string errorMessage { get; set; } = string.Empty;
        public bool stepCompleted  { get; set; }

        public bool hasStepContext =>
            documentType == ctxPostulation || documentType == ctxDeclaration;

        public async Task<IActionResult> OnGetAsync()
        {
            var planCheck = await _planValidator.VerificarPlanAsync();
            if (planCheck != null) return planCheck;

            if (hasStepContext)
                await LoadStepStatusAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var validationError = ValidateFile();
            if (validationError != null)
            {
                errorMessage = validationError;
                return Page();
            }

            var rnc = User.FindFirst("RNC")?.Value;
            if (string.IsNullOrEmpty(rnc))
            {
                errorMessage = "No hay empresa seleccionada. Selecciona una empresa antes de firmar.";
                return Page();
            }

            return await SignAndDownloadAsync(rnc);
        }

        public async Task<IActionResult> OnPostMarkStepCompletedAsync()
        {
            (int stepId, string stepName) = documentType switch
            {
                ctxPostulation => (1, "Postulación"),
                ctxDeclaration => (9, "Declaración Jurada"),
                _              => (-1, string.Empty)
            };

            if (stepId == -1)
                return new JsonResult(new { success = false, message = "Tipo de documento no válido." });

            try
            {
                var registered = await PasosCompletadosHelper.RegistrarPasoCompletado(
                    _httpClientFactory, _configuration, User, stepName, _logger, stepId);

                return new JsonResult(registered
                    ? new { success = true,  message = "¡Paso completado correctamente!" }
                    : new { success = false, message = "No se pudo registrar el paso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignDocuments] Error marking step {Id} as completed.", stepId);
                return new JsonResult(new { success = false, message = "Error inesperado al registrar el paso." });
            }
        }

        private async Task LoadStepStatusAsync()
        {
            var steps    = await _stepTrackingService.ObtenerPasosAsync(User);
            var stepName = documentType == ctxPostulation ? "Postulación" : "Declaración Jurada";

            stepCompleted = steps
                .FirstOrDefault(s => s.Name.Equals(stepName, StringComparison.OrdinalIgnoreCase))
                ?.Completed ?? false;
        }

        private string? ValidateFile()
        {
            if (xmlFile == null || xmlFile.Length == 0)
                return "Selecciona un archivo XML para firmar.";

            if (!xmlFile.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return "El archivo debe tener extensión .xml";

            if (xmlFile.Length > 5 * 1024 * 1024)
                return "El archivo supera el tamaño máximo permitido de 5 MB.";

            return null;
        }

        private async Task<IActionResult> SignAndDownloadAsync(string rnc)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                using var multipart  = new MultipartFormDataContent();
                using var fileStream = xmlFile!.OpenReadStream();

                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

                multipart.Add(fileContent, "XmlArchivo", xmlFile.FileName);
                multipart.Add(new StringContent(rnc), "RNC");

                _logger.LogInformation("[SignDocuments] Signing '{File}' | type={Type} | RNC={Rnc}",
                    xmlFile.FileName, documentType, rnc);

                var response = await client.PostAsync("api/FirmarXml/FirmarXml", multipart);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[SignDocuments] API returned {Status}: {Body}",
                        (int)response.StatusCode, body);

                    errorMessage = response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity
                        ? body.Trim('"')
                        : "No se pudo procesar la firma. Por favor, intenta nuevamente.";

                    return Page();
                }

                var signedBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("[SignDocuments] '{File}' signed successfully.", xmlFile.FileName);
                return File(signedBytes, "application/xml", xmlFile.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignDocuments] Unexpected error signing '{File}'.", xmlFile?.FileName);
                errorMessage = "Error inesperado al procesar la firma. Por favor, intenta más tarde.";
                return Page();
            }
        }
    }
}
