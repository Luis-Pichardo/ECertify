using eCertify.Services.Front;

namespace eCertify.Utils
{
    public class TokenHandler : DelegatingHandler
    {
        private readonly TokenAuthService _tokenAuthService;
        private readonly IHttpClientFactory _httpClientFactory;

        public TokenHandler(TokenAuthService tokenAuthService, IHttpClientFactory httpClientFactory)
        {
            _tokenAuthService = tokenAuthService;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            // No adjuntar token a endpoints de login/autenticación
            if (path.Contains("/api/authentication/validate-login", StringComparison.OrdinalIgnoreCase))
            {
                return await base.SendAsync(request, ct);
            }

            var token = await _tokenAuthService.ObtenerTokenUsuarioAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, ct);
        }


    }
}
