using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.DTOs.Front;
using eCertify.Utils;
using System.IO.Compression;
using eCertify.Services.Front;

namespace eCertify.Pages.Certificacion
{
    public class RepresentacionModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RepresentacionModel> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IPasosCompletadosService _pasosCompletadosService;
        private readonly IConfiguration _configuration;
        private readonly PlanValidator _planValidator;

        public List<ArchivoRepresentacionDTO> RepresentacionPdf { get; set; } = new();


        public RepresentacionModel(IHttpClientFactory httpClientFactory, ILogger<RepresentacionModel> logger, IWebHostEnvironment env, IPasosCompletadosService pasosCompletadosService, IConfiguration configuration, PlanValidator planValidator)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _env = env;
            _pasosCompletadosService = pasosCompletadosService;
            _configuration = configuration;
            _planValidator = planValidator;
        }

        public bool PasoCompletado { get; set; }

        public async Task OnGetPasosAsync()
        {
            var pasos = await _pasosCompletadosService.ObtenerPasosAsync(User);

            // Buscar el paso actual (por nombre o ID)
            var paso = pasos.FirstOrDefault(p =>
                p.Nombre.Equals("Pruebas Representaci�n Impresa", StringComparison.OrdinalIgnoreCase));

            PasoCompletado = paso?.Completado ?? false;

        }

        public async Task<IActionResult> OnGetAsync(string nombreArchivo)
        {
            var validacion = await _planValidator.VerificarPlanAsync();
            if (validacion != null)
                return validacion;

            await OnGetPasosAsync();

            var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);
            if (empresa == null || string.IsNullOrWhiteSpace(empresa.RNC))
            {
                _logger.LogWarning("Empresa no v�lida o RNC vac�o.");
                return Unauthorized();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                // Obtener token del usuario
                var token = User.FindFirst("Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                var response = await client.GetAsync($"api/RepresentacionImpresa/listar-pdfs-con-qr?rnc={empresa.RNC}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en la respuesta del API: {StatusCode}", response.StatusCode);
                    TempData["Error"] = "No se pudo obtener la lista de representaciones.";
                    RepresentacionPdf = new();
                    return Page();
                }

                var lista = await response.Content.ReadFromJsonAsync<List<ArchivoRepresentacionDTO>>();

                if (lista == null || lista.Count == 0)
                {
                    _logger.LogWarning("No se recibieron archivos PDF para el RNC {RNC}.", empresa.RNC);
                    TempData["Error"] = "No se encontraron archivos PDF generados.";
                    return Page();
                }

                RepresentacionPdf = lista;
                _logger.LogInformation("Se cargaron {Cantidad} PDFs para el RNC {RNC}.", RepresentacionPdf.Count, empresa.RNC);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los PDFs.");
                TempData["Error"] = "Error al obtener los PDFs.";
                RepresentacionPdf = new();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarcarCompletadoAsync()
        {
            const int pasoId = 5;
            const string pasoNombre = "Pruebas Representaci�n Impresa";

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

                    // Devolver JSON en lugar de recargar la p�gina
                    return new JsonResult(new { success = true, message = "�Paso completado correctamente!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No se pudo registrar el paso completado" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cr�tico al marcar paso como completado");
                return new JsonResult(new { success = false, message = "Ocurri� un error inesperado" });
            }
        }

        public async Task<IActionResult> OnGetDescargarPdfAsync(string nombreArchivo)
        {
            var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);
            if (empresa == null || string.IsNullOrWhiteSpace(empresa.RNC))
            {
                _logger.LogWarning("Empresa no v�lida o RNC vac�o.");
                return Unauthorized();
            }

            // Llama a la API para obtener el PDF
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                // Obtener token del usuario
                var token = User.FindFirst("Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                var url = $"/api/RepresentacionImpresa/descargar-pdf?rnc={empresa.RNC}&nombreArchivo={nombreArchivo}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("No se pudo descargar el PDF desde la API. C�digo: {Codigo}", response.StatusCode);
                    return NotFound("No se pudo obtener el archivo desde la API.");
                }

                var contenido = await response.Content.ReadAsByteArrayAsync();
                return File(contenido, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar PDF desde la API.");
                return StatusCode(500, "Error al descargar PDF.");
            }
        }



        // Descargar todos los PDFs en ZIP
        public async Task<IActionResult> OnGetDescargarZipAsync()
        {
            var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(User);
            if (empresa == null || string.IsNullOrWhiteSpace(empresa.RNC))
                return Unauthorized();

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                // Obtener token del usuario
                var token = User.FindFirst("Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                // 1. Obtener lista de PDFs
                var responseLista = await client.GetAsync($"api/RepresentacionImpresa/listar-pdfs-con-qr?rnc={empresa.RNC}");
                if (!responseLista.IsSuccessStatusCode)
                    return StatusCode((int)responseLista.StatusCode, "No se pudo obtener la lista de PDFs.");

                var listaPdf = await responseLista.Content.ReadFromJsonAsync<List<ArchivoRepresentacionDTO>>();
                if (listaPdf == null || listaPdf.Count == 0)
                    return NotFound("No se encontraron PDFs para descargar.");

                using var memoryStream = new MemoryStream();
                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true);

                // 2. Por cada archivo, descargarlo desde la API
                foreach (var pdf in listaPdf)
                {
                    var nombreArchivo = pdf.NombreArchivo;

                    var urlDescarga = $"/api/RepresentacionImpresa/descargar-pdf?rnc={empresa.RNC}&nombreArchivo={nombreArchivo}";

                    var responsePdf = await client.GetAsync(urlDescarga);
                    if (!responsePdf.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("No se pudo descargar el PDF {Archivo}", nombreArchivo);
                        continue; 
                    }

                    var contenidoPdf = await responsePdf.Content.ReadAsByteArrayAsync();

                    var entry = archive.CreateEntry(nombreArchivo, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(contenidoPdf);
                }

                archive.Dispose();
                memoryStream.Position = 0;

                var nombreZip = $"Representaciones_Impresas.zip";
                return File(memoryStream.ToArray(), "application/zip", nombreZip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el ZIP de PDFs.");
                return StatusCode(500, "Error al preparar la descarga.");
            }
        }



    }
}
