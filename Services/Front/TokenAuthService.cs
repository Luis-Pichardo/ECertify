using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace eCertify.Services.Front
{
    public class TokenAuthService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<TokenAuthService> _logger;

        public TokenAuthService(IHttpContextAccessor contextAccessor, ILogger<TokenAuthService> logger)
        {
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        public async Task<bool> ObtenerTokenProdAsync(HttpClient client)
        {
            var httpContext = _contextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("No se encontró HttpContext.");
                return false;
            }

            var result = await httpContext.AuthenticateAsync("EmpresaScheme");

            if (!result.Succeeded || result.Principal == null)
            {
                _logger.LogWarning("Autenticación fallida con el esquema EmpresaScheme.");
                return false;
            }

            var token = result.Principal.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }

            _logger.LogWarning("No se encontró token en los claims del usuario autenticado por EmpresaScheme.");
            return false;
        }

        public async Task<string?> ObtenerTokenUsuarioAsync()
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("No se encontró HttpContext.");
                return null;
            }

            // 1) Primero, identidad ya cargada en esta request
            var token = httpContext.User?.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
                return token;

            // 2) Fallback: autenticar con el esquema (lee desde la cookie)
            var result = await httpContext.AuthenticateAsync(AuthService.Esquema);
            if (result.Succeeded && result.Principal != null)
            {
                token = result.Principal.FindFirst("Token")?.Value;
                if (!string.IsNullOrEmpty(token))
                    return token;
            }

            _logger.LogWarning("No se encontró token en los claims del usuario autenticado.");
            return null;
        }

    }
}
