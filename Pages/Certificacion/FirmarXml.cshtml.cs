using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Services.Front;
using eCertify.Utils;
using System.Net.Http.Headers;

namespace eCertify.Pages.Certificacion
{
    public class FirmarXmlModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FirmarXmlModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPasosCompletadosService _pasosCompletadosService;
        private readonly PlanValidator _planValidator;

        public const string CTX_POSTULACION  = "postulacion";
        public const string CTX_DECLARACION  = "declaracion";

        public FirmarXmlModel(
            IHttpClientFactory httpClientFactory,
            ILogger<FirmarXmlModel> logger,
            IConfiguration configuration,
            IPasosCompletadosService pasosCompletadosService,
            PlanValidator planValidator)
        {
            _httpClientFactory       = httpClientFactory;
            _logger                  = logger;
            _configuration           = configuration;
            _pasosCompletadosService = pasosCompletadosService;
            _planValidator           = planValidator;
        }

        [BindProperty]
        public IFormFile ArchivoXml { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public string Ctx { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public bool PasoCompletado { get; set; }

        public bool TieneContextoDePaso =>
            Ctx == CTX_POSTULACION || Ctx == CTX_DECLARACION;

        public string TituloPagina => Ctx switch
        {
            CTX_POSTULACION => "Postulación",
            CTX_DECLARACION => "Declaración Jurada",
            _               => "Firmar Archivos"
        };

        public string SubtituloPagina => Ctx switch
        {
            CTX_POSTULACION => "Firma el XML de postulación generado en el portal DGII",
            CTX_DECLARACION => "Firma el XML de declaración jurada generado en el portal DGII",
            _               => "Firma cualquier archivo XML con tu certificado digital"
        };

        public string TituloDocumento => Ctx switch
        {
            CTX_POSTULACION => "XML de Postulación",
            CTX_DECLARACION => "XML de Declaración Jurada",
            _               => "Archivo XML"
        };

        public async Task<IActionResult> OnGetAsync()
        {
            var validacion = await _planValidator.VerificarPlanAsync();
            if (validacion != null) return validacion;

            if (TieneContextoDePaso)
            {
                var pasos = await _pasosCompletadosService.ObtenerPasosAsync(User);
                var nombre = Ctx == CTX_POSTULACION ? "Postulación" : "Declaración Jurada";
                PasoCompletado = pasos
                    .FirstOrDefault(p => p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))
                    ?.Completado ?? false;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ArchivoXml == null || ArchivoXml.Length == 0)
            {
                ErrorMessage = "Seleccione un archivo XML para firmar.";
                return Page();
            }

            if (!ArchivoXml.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "El archivo debe tener extensión .xml";
                return Page();
            }

            if (ArchivoXml.Length > 5 * 1024 * 1024)
            {
                ErrorMessage = "El archivo supera el tamaño máximo permitido de 5 MB.";
                return Page();
            }

            var rnc = User.FindFirst("RNC")?.Value;
            if (string.IsNullOrEmpty(rnc))
            {
                ErrorMessage = "No hay empresa seleccionada. Seleccione una empresa antes de firmar.";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                using var formData = new MultipartFormDataContent();
                using var stream   = ArchivoXml.OpenReadStream();
                var fileContent    = new StreamContent(stream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

                formData.Add(fileContent, "XmlArchivo", ArchivoXml.FileName);
                formData.Add(new StringContent(rnc), "RNC");

                _logger.LogInformation("Firmando {File} | ctx={Ctx} | RNC={Rnc}",
                    ArchivoXml.FileName, Ctx, rnc);

                var response = await client.PostAsync("api/FirmarXml/FirmarXml", formData);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error firma: {Status} – {Err}", response.StatusCode, err);
                    ErrorMessage = $"El servidor rechazó la solicitud: {err}";
                    return Page();
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Archivo {File} firmado correctamente.", ArchivoXml.FileName);
                return File(bytes, "application/xml", ArchivoXml.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al firmar {File}", ArchivoXml.FileName);
                ErrorMessage = $"Error inesperado: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            (int id, string nombre) = Ctx switch
            {
                CTX_POSTULACION => (1, "Postulación"),
                CTX_DECLARACION => (9, "Declaración Jurada"),
                _               => (-1, string.Empty)
            };

            if (id == -1)
                return new JsonResult(new { success = false, message = "Contexto no válido" });

            try
            {
                var ok = await PasosCompletadosHelper.RegistrarPasoCompletado(
                    _httpClientFactory, _configuration, User, nombre, _logger, id);

                return new JsonResult(ok
                    ? new { success = true,  message = "¡Paso completado correctamente!" }
                    : new { success = false, message = "No se pudo registrar el paso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar paso {Id}", id);
                return new JsonResult(new { success = false, message = "Error inesperado." });
            }
        }
    }
}
