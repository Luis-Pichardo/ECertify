using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace eCertify.Services.Front
{
    public class EmpresaAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<EmpresaAuthService> _logger;

        public EmpresaAuthService(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<EmpresaAuthService> logger)
        {
            _httpClient = httpClient;
            _contextAccessor = accessor;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage, string Rnc)> LoginEmpresaAsync(string correo, string contrasena)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/authentication/login", new
                {
                    Correo = correo,
                    Contrasena = contrasena
                });

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error, null);
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                var token = json.GetProperty("token").GetString();

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var empresaId = jwt.Claims.FirstOrDefault(c => c.Type == "EmpresaID")?.Value;
                var rnc = jwt.Claims.FirstOrDefault(c => c.Type == "RNC")?.Value;
                var razonSocial = jwt.Claims.FirstOrDefault(c => c.Type == "RazonSocial")?.Value;

                var claims = new List<Claim>
                {
                    new Claim("Token", token),
                    new Claim("EmpresaID", empresaId ?? ""),
                    new Claim("RNC", rnc ?? ""),
                    new Claim("RazonSocial", razonSocial ?? ""),
                    new Claim(ClaimTypes.Name, correo),
                    new Claim("Tipo", "Empresa")
                };

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await _contextAccessor.HttpContext.SignInAsync(
                    "EmpresaScheme",
                    new ClaimsPrincipal(new ClaimsIdentity(claims, "EmpresaScheme")),
                    authProperties);

                return (true, null, rnc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoginEmpresaAsync");
                return (false, "Error de conexión con el servidor de empresa", null);
            }

        }
    }
}
