using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using eCertify.Utils;

namespace eCertify.Pages
{
    public class HomeModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeModel> _logger;

        private static readonly IReadOnlyList<string> FixedStepNames = new[]
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

        public HomeModel(IHttpClientFactory httpClientFactory, ILogger<HomeModel> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _logger = logger;
        }

        public List<CertificationStepViewModel> Steps { get; set; } = new();
        public string CompanyName { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var company = ClaimHelper.ObtenerEmpresaDesdeClaims(User);
            CompanyName = company.RazonSocial;

            if (company.ID <= 0)
            {
                Response.Redirect("/CompanySelect");
                return;
            }

            var completedSteps = await FetchCompletedStepsAsync(company.ID);

            Steps = FixedStepNames.Select((name, index) =>
            {
                var match = completedSteps.FirstOrDefault(s =>
                    s.StepName.Equals(name, StringComparison.OrdinalIgnoreCase) && s.Completed);

                return new CertificationStepViewModel
                {
                    Id         = index + 1,
                    Name       = name,
                    Completed  = match != null,
                    CompletedOn = match?.CompletedOn
                };
            }).ToList();
        }

        private async Task<List<StepProgressDto>> FetchCompletedStepsAsync(long companyId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/HistorialPruebasExcel/por-empresa/{companyId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Step history API returned {Status} for company {CompanyId}",
                        response.StatusCode, companyId);
                    return new();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<StepProgressDto>>(json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch step history for company {CompanyId}", companyId);
                return new();
            }
        }
    }

    public class StepProgressDto
    {
        [JsonProperty("pasoId")]
        public int StepId { get; set; }

        [JsonProperty("pasoNombre")]
        public string StepName { get; set; } = string.Empty;

        [JsonProperty("completado")]
        public bool Completed { get; set; }

        [JsonProperty("fechaCompletado")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("porcentajeCompletado")]
        public int CompletionPercentage { get; set; }
    }

    public class CertificationStepViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime? CompletedOn { get; set; }
    }
}
