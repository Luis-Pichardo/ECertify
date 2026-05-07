using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using eCertify.Services.Front;

namespace eCertify.Pages
{
    public class CompanySelectModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CompanySelectModel> _logger;

        public CompanySelectModel(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<CompanySelectModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public string NombreUsuario { get; set; } = "";
        public string ApellidoUsuario { get; set; } = "";
        public int AccessToken { get; set; }
        public string? ErrorMessage { get; set; }

        public List<EmpresaDTO> Empresas { get; set; } = new();

        public bool PuedeCrearEmpresa => AccessToken == 0
            ? Empresas.Count < 1
            : Empresas.Count < AccessToken;

        public async Task OnGetAsync()
        {
            NombreUsuario   = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
            ApellidoUsuario = User.FindFirst("Apellido")?.Value ?? "";

            if (!User.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("[CompanySelect] Usuario no autenticado.");
                return;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("[CompanySelect] Claim NameIdentifier ausente en el usuario autenticado.");
                ErrorMessage = "No se pudo identificar al usuario. Inicia sesión nuevamente.";
                return;
            }

            if (!long.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("[CompanySelect] Claim NameIdentifier no es un long válido: '{Val}'", userIdClaim.Value);
                ErrorMessage = "Identificador de usuario inválido.";
                return;
            }

            _logger.LogInformation("[CompanySelect] Cargando datos para userId={UserId}", userId);

            var client = _httpClientFactory.CreateClient("ApiClient");

            // ── AccessToken del usuario ────────────────────────────────────────
            try
            {
                var userUrl = $"api/User/Buscar/{userId}";
                _logger.LogInformation("[CompanySelect] GET {Url}", userUrl);

                var userResp = await client.GetAsync(userUrl);
                _logger.LogInformation("[CompanySelect] api/User/Buscar → {Status}", (int)userResp.StatusCode);

                if (userResp.IsSuccessStatusCode)
                {
                    var body = await userResp.Content.ReadAsStringAsync();
                    _logger.LogDebug("[CompanySelect] api/User/Buscar body: {Body}", body);

                    var userObj = JsonSerializer.Deserialize<UserResponse>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    AccessToken = userObj?.Data?.AccessToken ?? 0;
                    _logger.LogInformation("[CompanySelect] AccessToken={AccessToken}", AccessToken);
                }
                else
                {
                    var errBody = await userResp.Content.ReadAsStringAsync();
                    _logger.LogWarning("[CompanySelect] api/User/Buscar falló {Status}: {Body}",
                        (int)userResp.StatusCode, errBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CompanySelect] Excepción al llamar api/User/Buscar/{UserId}", userId);
            }

            // ── Lista de empresas ──────────────────────────────────────────────
            try
            {
                var empresasUrl = $"api/Empresa/ListarPorUsuario/{userId}";
                _logger.LogInformation("[CompanySelect] GET {Url}", empresasUrl);

                var resp = await client.GetAsync(empresasUrl);
                _logger.LogInformation("[CompanySelect] api/Empresa/ListarPorUsuario → {Status}", (int)resp.StatusCode);

                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("[CompanySelect] api/Empresa/ListarPorUsuario body: {Body}", body);

                if (resp.IsSuccessStatusCode)
                {
                    var lista = JsonSerializer.Deserialize<List<EmpresaDTO>>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Empresas = lista ?? new List<EmpresaDTO>();
                    _logger.LogInformation("[CompanySelect] Empresas cargadas: {Count}", Empresas.Count);
                }
                else
                {
                    _logger.LogWarning("[CompanySelect] api/Empresa/ListarPorUsuario falló {Status}: {Body}",
                        (int)resp.StatusCode, body);
                    ErrorMessage = $"No se pudieron cargar las empresas (HTTP {(int)resp.StatusCode}).";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CompanySelect] Excepción al llamar api/Empresa/ListarPorUsuario/{UserId}", userId);
                ErrorMessage = "Error inesperado al cargar empresas. Revisa los logs.";
            }
        }

        public async Task<IActionResult> OnPostAsync(long empresaId)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("[CompanySelect] POST sin usuario autenticado.");
                return RedirectToPage("/Login");
            }

            _logger.LogInformation("[CompanySelect] Seleccionando empresaId={EmpresaId}", empresaId);

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var resp = await client.GetAsync($"api/Empresa/Buscar/{empresaId}");
                _logger.LogInformation("[CompanySelect] api/Empresa/Buscar/{Id} → {Status}", empresaId, (int)resp.StatusCode);

                if (!resp.IsSuccessStatusCode)
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("[CompanySelect] Error al obtener empresa {Id}: {Status} – {Body}",
                        empresaId, (int)resp.StatusCode, errBody);
                    ErrorMessage = "No se pudo obtener la empresa seleccionada.";
                    await OnGetAsync();
                    return Page();
                }

                var json    = await resp.Content.ReadAsStringAsync();
                var empresa = JsonSerializer.Deserialize<EmpresaDTO>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var razonSocial = empresa?.RazonSocial ?? "Empresa no disponible";
                var rnc         = empresa?.RNC         ?? "RNC no disponible";

                _logger.LogInformation("[CompanySelect] Empresa deserializada: ID={Id}, RazonSocial={RS}, RNC={RNC}",
                    empresa?.ID, razonSocial, rnc);

                // Recuperar token JWT almacenado en los claims de la cookie
                var token = User.FindFirst("Token")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    var authResult = await HttpContext.AuthenticateAsync("UsuarioScheme");
                    token = authResult.Principal?.FindFirst("Token")?.Value;
                    _logger.LogInformation("[CompanySelect] Token recuperado desde cookie: {Found}",
                        !string.IsNullOrEmpty(token));
                }

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[CompanySelect] No se encontró token JWT para renovar la sesión.");
                    ErrorMessage = "Sesión expirada. Inicia sesión nuevamente.";
                    return RedirectToPage("/Login");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""),
                    new Claim(ClaimTypes.Name,           User.FindFirstValue(ClaimTypes.Name)           ?? ""),
                    new Claim(ClaimTypes.Email,          User.FindFirstValue(ClaimTypes.Email)          ?? ""),
                    new Claim("Apellido",        User.FindFirstValue("Apellido")        ?? ""),
                    new Claim("EmpresaId",       empresaId.ToString()),
                    new Claim("RazonSocial",     razonSocial),
                    new Claim("RNC",             rnc),
                    new Claim("DireccionEmpresa", empresa?.Direccion ?? ""),
                    new Claim("Token",           token)
                };

                var identity  = new ClaimsIdentity(claims, "UsuarioScheme");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("UsuarioScheme", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(2)
                });

                _logger.LogInformation("[CompanySelect] Sesión actualizada. Redirigiendo a /Home.");
                return RedirectToPage("/Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CompanySelect] Excepción en OnPostAsync para empresaId={EmpresaId}", empresaId);
                ErrorMessage = "Error inesperado al seleccionar la empresa.";
                await OnGetAsync();
                return Page();
            }
        }

        // ── DTOs locales ──────────────────────────────────────────────────────
        public class EmpresaDTO
        {
            public long    ID          { get; set; }
            public string? RNC         { get; set; }
            public string? RazonSocial { get; set; }
            public string? Direccion   { get; set; }
        }

        public class UserResponse
        {
            public string?   Message { get; set; }
            public UserData? Data    { get; set; }
        }

        public class UserData
        {
            public string?   Email       { get; set; }
            public DateTime  CreatedDate { get; set; }
            public string?   Password    { get; set; }
            public int       AccessToken { get; set; }
        }
    }
}
