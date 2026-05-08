using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Utils;
using System.Text.Json;
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

        [BindProperty]
        public IFormFile ExcelFile { get; set; }
        public List<FacturaDTO> Facturas { get; set; } = new();
        public List<FacturaDTO> Resumenes { get; set; } = new();

        public bool PasoCompletado { get; set; }

        public async Task OnGetPasosAsync()
        {
            var pasos = await _pasosCompletadosService.ObtenerPasosAsync(User);

            // Buscar el paso actual (por nombre o ID)
            var paso = pasos.FirstOrDefault(p =>
                p.Name.Equals("Pruebas de Datos e-CF", StringComparison.OrdinalIgnoreCase));

            PasoCompletado = paso?.Completed ?? false;
        }

        public async Task OnGet()
        {
            try
            {
                try
                {
                    await OnGetPasosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener pasos, pero continuo cargando facturas");
                    PasoCompletado = false;
                }

                // Obtener RNC del usuario autenticado
                var rnc = User.FindFirst("RNC")?.Value;

                if (string.IsNullOrWhiteSpace(rnc))
                {
                    _logger.LogWarning("Usuario no tiene RNC en sus claims");
                    Facturas = new List<FacturaDTO>();
                    return;
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync($"api/PruebasExcel/ListarXmlsGenerados?rnc={rnc}");

                response.EnsureSuccessStatusCode(); // Lanza excepci�n si no es 200-299

                var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (!responseData.TryGetProperty("archivos", out var archivos) || archivos.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Respuesta no contiene array de archivos v�lido");
                    Facturas = new List<FacturaDTO>();
                    return;
                }

                Facturas = new List<FacturaDTO>();
                int index = 1;

                foreach (var archivo in archivos.EnumerateArray())
                {
                    try
                    {
                        // Manejo seguro de propiedades JSON
                        string nombreArchivo = archivo.TryGetProperty("nombreArchivo", out var nombreProp)
                        ? nombreProp.GetString() ?? "Archivo sin nombre"
                        : "Archivo sin nombre";

                        bool puedeDescargar = archivo.TryGetProperty("puedeDescargarse", out var descargaProp)
                            && descargaProp.GetBoolean();


                        // Remover el RNC del nombre si est� presente
                        if (nombreArchivo.StartsWith(rnc))
                        {
                            nombreArchivo = nombreArchivo[rnc.Length..];
                        }

                        Facturas.Add(new FacturaDTO
                        {
                            Numero = index++,
                            Factura = nombreArchivo,
                            FileNameCompleto = rnc + nombreArchivo,
                            Estado = "No Enviado",
                            PuedeDescargarse = puedeDescargar
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando un archivo en la respuesta");
                        // Continuar con el siguiente archivo si hay error en uno
                    }
                }

                _logger.LogInformation("Se cargaron {Count} facturas correctamente", Facturas.Count);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error al llamar al API para obtener facturas");
                Facturas = new List<FacturaDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cargar facturas");
                Facturas = new List<FacturaDTO>();
            }
        }

        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            const int pasoId = 2;
            const string pasoNombre = "Pruebas de Datos e-CF";

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

                    // Devolver JSON en lugar de recargar la página
                    return new JsonResult(new { success = true, message = "¡Paso completado correctamente!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No se pudo registrar el paso completado" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al marcar paso como completado");
                return new JsonResult(new { success = false, message = "Ocurrió un error inesperado" });
            }
        }
    }

    public class FacturaDTO
    {
        public int Numero { get; set; }
        public string Factura { get; set; }
        public string FileNameCompleto { get; set; }
        public string Estado { get; set; }
        public bool PuedeDescargarse { get; set; }
    }

    public class HistorialPruebasExcel
    {
        public string ArchivoXml { get; set; }
        public string EstadoEnvio { get; set; }
        public DateTime FechaEnvio { get; set; }
        // Puedes agregar más campos si los necesitas
    }

}
