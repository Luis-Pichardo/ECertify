using Newtonsoft.Json;
using eCertify.Pages;
using eCertify.Utils;
using System.Security.Claims;
using eCertify.DTOs.Front;

namespace eCertify.Services.Front
{
    public class PasosCompletadosService : IPasosCompletadosService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PasosCompletadosService> _logger;

        public PasosCompletadosService(IHttpClientFactory httpClientFactory, ILogger<PasosCompletadosService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<List<PasoViewModel>> ObtenerPasosAsync(ClaimsPrincipal user)
        {
            var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(user);

            if (empresa.ID <= 0)
            {
                _logger.LogWarning("Empresa inválida o sin ID en claims. Usuario: {User}", user.Identity?.Name);
                return new List<PasoViewModel>();
            }

            var listaFija = new List<string>
            {
                "Postulación",
                "Pruebas de Datos e-CF",
                "Aprobación Comercial",
                "Pruebas Simulación e-CF",
                "Pruebas Representación Impresa",
                "URL Servicios Prueba",
                "Recepción e-CF",
                "Recepción Aprobación Comercial",
                "Declaración Jurada"
            };

            var client = _httpClientFactory.CreateClient("ApiClient");
            var url = $"api/HistorialPruebasExcel/por-empresa/{empresa.ID}";

            _logger.LogInformation("Obteniendo pasos para empresa {EmpresaId} desde {Url}", empresa.ID, url);

            List<PasosCompletadosDTO> completados = new();

            try
            {
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    completados = JsonConvert.DeserializeObject<List<PasosCompletadosDTO>>(content) ?? new();
                }
                else
                {
                    _logger.LogWarning("Error HTTP {StatusCode} al obtener pasos para empresa {EmpresaId}: {Content}",
                        response.StatusCode, empresa.ID, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pasos completados para empresa {EmpresaId}", empresa.ID);
            }

            // Generar lista final de pasos
            var resultado = listaFija.Select((nombre, index) =>
            {
                var encontrado = completados.FirstOrDefault(c =>
                    c.PasoNombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) && c.Completado);

                return new PasoViewModel
                {
                    Id = index + 1,
                    Nombre = nombre,
                    Completado = encontrado != null,
                    FechaCompletado = encontrado?.FechaCompletado
                };
            }).ToList();

            return resultado;
        }
    }
}
