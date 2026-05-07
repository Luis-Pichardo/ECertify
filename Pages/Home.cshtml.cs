using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using eCertify.Utils;

namespace eCertify.Pages
{
    public class HomeModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public HomeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public List<PasoViewModel> Pasos { get; set; } = new();

        public async Task OnGetAsync(long empresaId)
        {

            // Obtener la empresa desde los claims del usuario
            var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);

            if (empresa.ID <= 0)
            {
                // Redirigir a selecci�n de empresa si no hay ninguna v�lida
                Response.Redirect("/CompanySelect");
                return;
            }


            // Lista de pasos fijos
            var listaFija = new List<string>
            {
                "Postulaci�n",
                "Pruebas de Datos e-CF",
                "Aprobaci�n Comercial",
                "Pruebas Simulaci�n e-CF",
                "Pruebas Representaci�n Impresa",
                "URL Servicios Prueba",
                "Recepci�n e-CF",
                "Recepci�n Aprobaci�n Comercial",
                "Declaraci�n Jurada"
            };

            // Llamada al API
            var response = await _httpClient.GetAsync($"api/HistorialPruebasExcel/por-empresa/{empresa.ID}");
            var content = await response.Content.ReadAsStringAsync();

            List<PasoCompletadoDto> completados = new();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    completados = JsonConvert.DeserializeObject<List<PasoCompletadoDto>>(content) ?? new();
                }
                catch (JsonException ex)
                {
                    // Loggea o maneja el error de deserializaci�n
                    Console.WriteLine($"Error deserializando la respuesta: {ex.Message}");
                }
            }
            else
            {
                // Manejar error del API
                Console.WriteLine($"Error {response.StatusCode}: {content}");
            }

            // Armar el modelo comparando lista fija con completados
            Pasos = listaFija.Select((nombre, index) =>
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
        }
    }

    public class PasoCompletadoDto
    {
        public int PasoId { get; set; }
        public string PasoNombre { get; set; }
        public bool Completado { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public int PorcentajeCompletado { get; set; }
    }

    public class PasoViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Completado { get; set; }
        public DateTime? FechaCompletado { get; set; }
    }
}
