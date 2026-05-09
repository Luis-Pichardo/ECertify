using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Utils;
using eCertify.Services.Front;

namespace eCertify.Pages.Certificacion
{
    [Authorize]
    public class PruebasModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PruebasModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPasosCompletadosService _pasosCompletadosService;

        public PruebasModel(
            IHttpClientFactory httpClientFactory,
            ILogger<PruebasModel> logger,
            IConfiguration configuration,
            IPasosCompletadosService pasosCompletadosService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _pasosCompletadosService = pasosCompletadosService;
        }

        public bool StepCompleted { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var steps = await _pasosCompletadosService.ObtenerPasosAsync(User);
                var step = steps.FirstOrDefault(s =>
                    s.Name.Equals("Pruebas de Datos e-CF", StringComparison.OrdinalIgnoreCase));

                StepCompleted = step?.Completed ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado del paso Pruebas");
                StepCompleted = false;
            }
        }

        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            const int stepId = 2;
            const string stepName = "Pruebas de Datos e-CF";

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
                    _logger.LogInformation("Paso {StepId} ({StepName}) registrado para empresa {CompanyId}",
                        stepId, stepName, ClaimHelper.ObtenerEmpresaDesdeClaims(User).ID);

                    return new JsonResult(new { success = true, message = "¡Paso completado correctamente!" });
                }

                return new JsonResult(new { success = false, message = "No se pudo registrar el paso completado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar paso {StepId} como completado", stepId);
                return new JsonResult(new { success = false, message = "Ocurrió un error inesperado" });
            }
        }
    }
}
