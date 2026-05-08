using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using eCertify.Services.Front;
using eCertify.Utils;
using System.Net.Http;

namespace eCertify.Pages.Certificacion
{
    public class UrlserviciosModel : PageModel
    {
        private readonly IPasosCompletadosService _pasosCompletadosService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UrlserviciosModel> _logger;
        private readonly PlanValidator _planValidator;

        public UrlserviciosModel(IPasosCompletadosService pasosCompletadosService, IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<UrlserviciosModel> logger, PlanValidator planValidator)
        {
            _pasosCompletadosService = pasosCompletadosService;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _planValidator = planValidator;
        }
        public bool PasoCompletado { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var validacion = await _planValidator.VerificarPlanAsync();
            if (validacion != null)
                return validacion;

            var pasos = await _pasosCompletadosService.ObtenerPasosAsync(User);

            // Definir los pasos que quieres validar
            var pasosAValidar = new List<(int Id, string Nombre)>
            {
                (6, "URL Servicios Prueba"),
                (7, "Recepci�n e-CF"),
                (8, "Recepci�n Aprobaci�n Comercial")
            };

            // Verificar si todos est�n completados
            var todosCompletados = pasosAValidar.All(p =>
                pasos.Any(x =>
                    x.Id == p.Id &&
                    x.Name.Equals(p.Nombre, StringComparison.OrdinalIgnoreCase) &&
                    x.Completed));

            PasoCompletado = todosCompletados;

            return Page();
        }


        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            var pasos = new List<(int PasoId, string PasoNombre)>
            {
                (6, "URL Servicios Prueba"),
                (7, "Recepci�n e-CF"),
                (8, "Recepci�n Aprobaci�n Comercial")
            };

            var errores = new List<string>();

            try
            {
                foreach (var paso in pasos)
                {
                    var resultado = await PasosCompletadosHelper.RegistrarPasoCompletado(
                        _httpClientFactory,
                        _config,
                        User,
                        paso.PasoNombre,
                        _logger,
                        paso.PasoId);

                    if (!resultado)
                        errores.Add($"No se pudo registrar el paso {paso.PasoNombre}");
                }

                if (errores.Any())
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = string.Join(" | ", errores)
                    });
                }

                return new JsonResult(new
                {
                    success = true,
                    message = "�Todos los pasos completados correctamente!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cr�tico al marcar pasos como completados");
                return new JsonResult(new { success = false, message = "Ocurri� un error inesperado" });
            }
        }

    }
}
