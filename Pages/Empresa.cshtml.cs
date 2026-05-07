using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using eCertify.DTOs.Front;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace eCertify.Pages
{
    public class EmpresaModel : PageModel
    {
        private readonly SogeDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmpresaModel> _logger;

        public EmpresaModel(
            SogeDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<EmpresaModel> logger)
        {
            _context         = context;
            _httpClientFactory = httpClientFactory;
            _logger          = logger;
        }

        [BindProperty]
        public EmpresaUploadDTO Empresa { get; set; } = new EmpresaUploadDTO();

        public List<ProvinciaConMunicipios> ProvinciasConMunicipios { get; set; } = new();

        public string? ErrorMessage  { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Consulta directa a BD — sin llamada HTTP ni autorización necesaria
            ProvinciasConMunicipios = await _context.Provincias
                .Include(p => p.Municipios)
                .OrderBy(p => p.Descripcion)
                .Select(p => new ProvinciaConMunicipios
                {
                    Id          = p.Prov_Id,
                    Descripcion = p.Descripcion,
                    Municipios  = p.Municipios
                        .OrderBy(m => m.Descripcion)
                        .Select(m => new DTOs.Front.MunicipioDTO
                        {
                            Id          = m.Muni_Id,
                            Descripcion = m.Descripcion
                        }).ToList()
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor corrige los errores en el formulario.";
                await RecargarProvincias();
                return Page();
            }

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(Empresa.RNC          ?? ""), "RNC");
                content.Add(new StringContent(Empresa.RazonSocial  ?? ""), "RazonSocial");
                content.Add(new StringContent(Empresa.CertificadoPass ?? ""), "CertificadoPass");
                content.Add(new StringContent(Empresa.Status.ToString()),   "Status");

                if (!string.IsNullOrEmpty(userId))
                    content.Add(new StringContent(userId), "UserId");

                if (Empresa.SuscripcionID.HasValue)
                    content.Add(new StringContent(Empresa.SuscripcionID.Value.ToString()), "SuscripcionID");

                if (!string.IsNullOrWhiteSpace(Empresa.Email))
                    content.Add(new StringContent(Empresa.Email), "Email");

                if (!string.IsNullOrWhiteSpace(Empresa.Direccion))
                    content.Add(new StringContent(Empresa.Direccion), "Direccion");

                content.Add(new StringContent(Empresa.ProvinciaId?.ToString() ?? ""), "ProvinciaId");
                content.Add(new StringContent(Empresa.MunicipioId?.ToString() ?? ""), "MunicipioId");

                await AgregarArchivo(Empresa.Certificado,  "Certificado",  content);
                await AgregarArchivo(Empresa.LogoArchivo,  "LogoArchivo",  content);
                await AgregarArchivo(Empresa.ExcelPruebas, "ExcelPruebas", content);

                var client   = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.PostAsync("api/Empresa/Agregar", content);

                if (response.IsSuccessStatusCode)
                    return RedirectToPage("/CompanySelect");

                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error al crear empresa. {Status} – {Error}", response.StatusCode, err);
                ErrorMessage = $"Error del servidor: {err}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar empresa");
                ErrorMessage = "Error inesperado. Por favor intente nuevamente.";
            }

            await RecargarProvincias();
            return Page();
        }

        private async Task RecargarProvincias()
        {
            ProvinciasConMunicipios = await _context.Provincias
                .Include(p => p.Municipios)
                .OrderBy(p => p.Descripcion)
                .Select(p => new ProvinciaConMunicipios
                {
                    Id          = p.Prov_Id,
                    Descripcion = p.Descripcion,
                    Municipios  = p.Municipios
                        .OrderBy(m => m.Descripcion)
                        .Select(m => new DTOs.Front.MunicipioDTO
                        {
                            Id          = m.Muni_Id,
                            Descripcion = m.Descripcion
                        }).ToList()
                })
                .ToListAsync();
        }

        private static async Task AgregarArchivo(
            IFormFile? file, string campo, MultipartFormDataContent content)
        {
            if (file is null || file.Length == 0) return;
            var stream      = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, campo, file.FileName);
        }
    }
}
