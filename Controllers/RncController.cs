using Microsoft.AspNetCore.Mvc;

namespace eCertify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RncController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RncController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("consulta")]
        public async Task<IActionResult> ConsultarPorRnc([FromQuery] string rnc)
        {
            var digits = new string((rnc ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length != 9 && digits.Length != 11)
                return BadRequest(new { error = true, mensaje = "El RNC debe tener exactamente 9 u 11 dígitos." });

            var client = _httpClientFactory.CreateClient("RncApiClient");
            var response = await client.GetAsync($"/api/consulta?rnc={digits}");
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet("nombres")]
        public async Task<IActionResult> BuscarPorNombre([FromQuery] string buscar)
        {
            var term = buscar?.Trim() ?? string.Empty;
            if (term.Length < 3)
                return BadRequest(new { error = true, mensaje = "El término debe tener al menos 3 caracteres." });

            var client = _httpClientFactory.CreateClient("RncApiClient");
            var response = await client.GetAsync($"/api/consulta/nombres?buscar={Uri.EscapeDataString(term)}");
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
    }
}
