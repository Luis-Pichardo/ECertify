using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace eCertify.Services.Front
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public const string Esquema = "UsuarioScheme";

        public AuthService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _httpClient = httpClient;

            // Verificación mejorada
            if (_httpClient.BaseAddress == null)
            {
                logger.LogError("FATAL: HttpClient BaseAddress no configurado");
                throw new InvalidOperationException("HttpClient BaseAddress no configurado. Verifica la configuración en Program.cs");
            }

            logger.LogInformation("HttpClient configurado para: {BaseAddress}", _httpClient.BaseAddress);
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage)> LoginAsync(string email, string password)
        {
            try
            {
                // Validar credenciales con la API pública
                var response = await _httpClient.PostAsJsonAsync(
                    "api/authentication/validate-login",
                    new { Email = email, Password = password });

                if (!response.IsSuccessStatusCode)
                {
                    var rawError = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Login fallido para {Email} — HTTP {Status}: {Error}",
                        email, (int)response.StatusCode, rawError);

                    // Nunca exponer errores técnicos del servidor al usuario
                    var userMessage = (int)response.StatusCode >= 500
                        ? "Ocurrió un error al procesar la solicitud. Por favor intente más tarde."
                        : "Correo o contraseña incorrectos.";

                    return (false, userMessage);
                }

                // 3️⃣ Leer todo el contenido una sola vez
                var content = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, options);

                if (loginResponse == null || loginResponse.User == null || string.IsNullOrEmpty(loginResponse.Token))
                {
                    _logger.LogWarning("No se pudo parsear la respuesta o faltan datos para {Email}", email);
                    return (false, "Error al obtener datos del usuario o token");
                }

                // Usa loginResponse.User y loginResponse.Token
                var userData = loginResponse.User;
                var token = loginResponse.Token;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userData.Id.ToString()),
                    new Claim(ClaimTypes.Email, userData.Email ?? string.Empty),
                    new Claim(ClaimTypes.Name, userData.Name ?? string.Empty),
                    new Claim("Apellido", userData.LastName ?? string.Empty),
                    new Claim("Token", token)
                };

                // Crear cookie de autenticación
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await _httpContextAccessor.HttpContext.SignInAsync(
                    Esquema,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema)),
                    authProperties);

                _logger.LogInformation("Login exitoso para {Email}", email);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login");
                return (false, "Error de conexión con el servidor");
            }
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(Esquema);
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public UserData User { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }   
        public string Email { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
    }
}
