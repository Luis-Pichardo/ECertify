using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using eCertify.DTOs.Front;
using System.Text.Json;
using eCertify.Services.Front;

namespace eCertify.Pages
{
    public class EmpresaProdModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmpresaProdModel> _logger;
        private readonly TokenAuthService _tokenService;
        private readonly IConfiguration _configuration;

        public EmpresaProdModel(IHttpClientFactory httpClientFactory, ILogger<EmpresaProdModel> logger, TokenAuthService tokenService, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        [BindProperty]
        public EmpresaProdUploadDTO Empresa { get; set; } = new EmpresaProdUploadDTO();

        public List<ProvinciaConMunicipios> ProvinciasConMunicipios { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty]
        public string? ClaveUsuario { get; set; }

        public bool AccesoPermitido
        {
            get => HttpContext.Session.GetString("AccesoEmpresaProd") == "true";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!AccesoPermitido)
                {
                    return Page();
                }

                var client = _httpClientFactory.CreateClient("ApiClientProd");

                var success = await _tokenService.ObtenerTokenProdAsync(client);
                if (!success)
                {
                    return RedirectToPage("/Login");
                }

                var response = await client.GetAsync("api/Provincia/ListarConMunicipios");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ProvinciasConMunicipios = JsonSerializer.Deserialize<List<ProvinciaConMunicipios>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ProvinciaConMunicipios>();
                }
                else
                {
                    ErrorMessage = "Error al cargar las provincias y municipios.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener provincias y municipios.");
                ErrorMessage = "Ocurri� un error inesperado al cargar las provincias y municipios.";
            }

            return Page();
        }

        public IActionResult OnPostClave()
        {
            var claveCorrecta = _configuration["ClaveAccesoEmpresaProd"];

            if (ClaveUsuario == claveCorrecta)
            {
                HttpContext.Session.SetString("AccesoEmpresaProd", "true");
                return RedirectToPage();
            }

            ErrorMessage = "Clave incorrecta.";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor corrige los errores en el formulario e intenta de nuevo.";
                return Page();
            }

            try
            {
                using var content = new MultipartFormDataContent();

                // Campos simples
                content.Add(new StringContent(Empresa.RNC), "RNC");
                content.Add(new StringContent(Empresa.RazonSocial), "RazonSocial");
                content.Add(new StringContent(Empresa.CertificadoPass), "CertificadoPass");
                content.Add(new StringContent(Empresa.Status.ToString()), "Status");
                content.Add(new StringContent(Empresa.Correo.ToString()), "Correo");
                content.Add(new StringContent(Empresa.Contrasena.ToString()), "Contrasena");

                // Campos opcionales

                if (!string.IsNullOrWhiteSpace(Empresa.Email))
                    content.Add(new StringContent(Empresa.Email), "Email");

                if (!string.IsNullOrWhiteSpace(Empresa.Direccion))
                    content.Add(new StringContent(Empresa.Direccion), "Direccion");

                content.Add(new StringContent(Empresa.ProvinciaId.ToString()), "ProvinciaId");
                content.Add(new StringContent(Empresa.MunicipioId.ToString()), "MunicipioId");

                // Archivos
                await ProcessFileUpload(Empresa.Certificado, "Certificado", content);
                await ProcessFileUpload(Empresa.LogoArchivo, "LogoArchivo", content);

                var client = _httpClientFactory.CreateClient("ApiClientProd");
                var success = await _tokenService.ObtenerTokenProdAsync(client);
                if (!success)
                {
                    return RedirectToPage("/Login");
                }
                var response = await client.PostAsync("api/Empresa/Agregar", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToPage("/ConsultarFacturas", new { rnc = Empresa.RNC });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error al crear empresa. C�digo: {StatusCode}, Respuesta: {Error}",
                    response.StatusCode, errorContent);

                ErrorMessage = $"Error del servidor: {errorContent}";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar datos al servidor");
                ErrorMessage = "Error de conexi�n con el servidor. Por favor intente nuevamente.";
                return Page();
            }
        }

        private async Task ProcessFileUpload(IFormFile file, string fieldName, MultipartFormDataContent content)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    var stream = file.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    content.Add(fileContent, fieldName, file.FileName);
                    _logger.LogDebug("Archivo {FileName} agregado al formulario como {FieldName}", file.FileName, fieldName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar el archivo {FileName}", file?.FileName);
                    throw new Exception($"Error al procesar el archivo {file?.FileName}. Por favor intente con otro archivo.");
                }
            }
        }
    }
}
