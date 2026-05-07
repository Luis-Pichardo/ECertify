using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using eCertify.Interfaces;
using eCertify.Models;

namespace eCertify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HistorialPagoController : ControllerBase
    {
        private readonly IHistorialPagoService _pagoService;

        public HistorialPagoController(IHistorialPagoService pagoService)
        {
            _pagoService = pagoService;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] HistorialPago pago)
        {
            var resultado = await _pagoService.RegistrarPagoAsync(pago);
            if (resultado == null)
                return StatusCode(500, "Ocurrió un error al registrar el pago.");

            return Ok(resultado);
        }

        [HttpGet("{userId:long}/{empresaId:long}")]
        public async Task<IActionResult> ObtenerPagos(long userId, long empresaId)
        {
            var pagos = await _pagoService.ObtenerPagosAsync(userId, empresaId);
            if (pagos == null || !pagos.Any())
            {
                return NotFound(new { mensaje = "No existe pago registrado con dicha empresa." });
            }
            return Ok(pagos);
        }
    }
}
