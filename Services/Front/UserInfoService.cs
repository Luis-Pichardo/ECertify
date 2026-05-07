using System.Security.Claims;
using System.Text.Json;

namespace eCertify.Services.Front
{
    public class UserInfoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserInfoService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserDto> ObtenerUsuarioActualAsync()
        {
            var user = _httpContextAccessor.HttpContext.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return null;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return null;

            var client = _httpClientFactory.CreateClient("ApiClient");

            var response = await client.GetAsync($"api/User/Buscar/{userId}");
            response.EnsureSuccessStatusCode();

            var userJson = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<ApiResponse<UserDto>>(
                userJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var usuario = apiResult?.Data;
            if (usuario == null) return null;

            var empresaResponse = await client.GetAsync($"api/Empresa/ListarPorUsuario/{userId}");
            empresaResponse.EnsureSuccessStatusCode();

            var empresasJson = await empresaResponse.Content.ReadAsStringAsync();
            var empresas = JsonSerializer.Deserialize<List<EmpresaInfoDto>>(
                empresasJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            usuario.EmpresaCount = empresas?.Count ?? 0;

            return usuario;
        }
    }

    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class EmpresaInfoDto
    {
        public long Id { get; set; }
        public string Rnc { get; set; }
        public string RazonSocial { get; set; }
        public int UserId { get; set; }
    }

    public class UserDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int AccessToken { get; set; }
        public int EmpresaCount { get; set; }
    }
}
