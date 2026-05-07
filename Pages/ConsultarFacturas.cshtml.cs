using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.DTOs.Front;
using eCertify.Utils;

namespace eCertify.Pages
{
    [Authorize(AuthenticationSchemes = "EmpresaScheme")]
    public class ConsultarFacturasModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ConsultarFacturasModel> _logger;

        public ConsultarFacturasModel(IHttpClientFactory httpClientFactory, ILogger<ConsultarFacturasModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Rnc { get; set; }
        public List<FacturaHistorialDTO> Facturas { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Rnc))
                {
                    _logger.LogWarning("RNC no proporcionado en la ruta");
                    return;
                }

                var client = _httpClientFactory.CreateClient("ApiClientProd");
                // Obtener token desde claims del usuario autenticado
                var token = User.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    _logger.LogWarning("No se encontró token en los claims del usuario.");
                    return;
                }
                var response = await client.GetAsync($"api/FacturasXml/Produccion/Consultar/Facturas?rnc={Rnc}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HistorialApiResponse>();

                    if (result?.Success == true)
                    {
                        Facturas = result.Data;
                    }
                }
                else
                {
                    _logger.LogWarning("No se pudo obtener historial. Código: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consumir historial de facturas");
            }
        }

        public class HistorialApiResponse
        {
            public bool Success { get; set; }
            public int Total { get; set; }
            public List<FacturaHistorialDTO> Data { get; set; }
        }

    }
}
