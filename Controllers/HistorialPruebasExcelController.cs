using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using eCertify.Interfaces;
using eCertify.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace eCertify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialPruebasExcelController : ControllerBase
    {
        private readonly IHistorialPruebasExcelService _historialService;
        private readonly ILogger<HistorialPruebasExcelController> _logger;
        private readonly SogeDbContext _context;

        public HistorialPruebasExcelController(
            IHistorialPruebasExcelService historialService,
            ILogger<HistorialPruebasExcelController> logger, SogeDbContext context)
        {
            _historialService = historialService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Registra un nuevo envío en el historial.
        /// </summary>
        [HttpPost("Registrar")]
        public async Task<IActionResult> RegistrarHistorial([FromBody] HistorialPruebasExcel historial)
        {
            if (historial == null)
            {
                _logger.LogWarning("Intento de registrar historial con datos nulos.");
                return BadRequest("Datos inválidos.");
            }

            try
            {
                await _historialService.RegistrarEnvioAsync(historial);
                _logger.LogInformation("Historial registrado correctamente: RNC={RNC}, Tipo={Tipo}, eNCF={eNCF}",
                    historial.Rnc, historial.Tipo, historial.Encf);

                return Ok(new { message = "Historial registrado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar historial.");
                return StatusCode(500, "Error interno al registrar historial.");
            }
        }

        /// <summary>
        /// Consulta el historial por Id único.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ConsultarPorId(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Consulta de historial con ID inválido: {Id}", id);
                return BadRequest("El ID debe ser mayor que cero.");
            }

            try
            {
                var historial = await _historialService.ObtenerHistorialPorIdAsync(id);

                if (historial == null)
                {
                    _logger.LogInformation("No se encontró historial con ID: {Id}", id);
                    return NotFound("No se encontró historial con ese ID.");
                }

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar historial por ID.");
                return StatusCode(500, "Error interno al consultar historial.");
            }
        }

        /// <summary>
        /// Consulta el historial por nombre de archivo XML completo (archivoXml).
        /// </summary>
        [HttpGet("ListarUltimosAceptados")]
        public async Task<IActionResult> ListarUltimosAceptados()
        {
            try
            {
                // Obtener el último registro por cada archivo donde el estado sea "Aceptado"
                var ultimosAceptados = await _historialService
                    .ObtenerTodosAsync() // o método para traer todos los registros
                    .ContinueWith(t => t.Result
                        .GroupBy(h => h.ArchivoXml)
                        .Select(g => g
                            .OrderByDescending(x => x.FechaEnvio)
                            .FirstOrDefault(h => h.EstadoEnvio != null && h.EstadoEnvio.Trim().Equals("Aceptado", StringComparison.OrdinalIgnoreCase))
                        )
                        .Where(x => x != null)
                        .ToList()
                    );

                return Ok(ultimosAceptados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando últimos archivos aceptados");
                return StatusCode(500, "Error interno al obtener archivos aceptados");
            }
        }

        //Aqui agregare el EndPoint para guardar la informacion de los PasosCompletados para no hacer otro controlador...
        [HttpPost("RegistrarPasos")]
        public async Task<IActionResult> RegistrarPasoCompletado([FromBody] RegistrarPasoCompletadoDto dto)
        {
            try
            {
                // Obtener UserId del token (ajustamos a "sub" si no hay NameIdentifier)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (userIdClaim == null)
                    return Unauthorized(new { Mensaje = "Usuario no autenticado" });

                if (!long.TryParse(userIdClaim.Value, out long userId))
                    return Unauthorized(new { Mensaje = "UserId inválido en token" });

                // Verificar que la empresa exista y pertenezca al usuario
                var empresa = await _context.Empresas
                    .FirstOrDefaultAsync(e => e.ID == dto.EmpresaId && e.UserId == userId);

                if (empresa == null)
                    return NotFound(new { Mensaje = "Empresa no encontrada o no pertenece al usuario" });

                // Buscar registro existente del paso
                var pasoExistente = await _context.PasosCompletados
                    .FirstOrDefaultAsync(p => p.EmpresaId == dto.EmpresaId
                                          && p.UserId == userId
                                          && p.PasoId == dto.PasoId);

                if (pasoExistente == null)
                {
                    // Crear nuevo registro
                    var nuevoPaso = new PasoCompletado
                    {
                        EmpresaId = dto.EmpresaId,
                        UserId = userId,
                        PasoId = dto.PasoId,
                        PasoNombre = dto.PasoNombre,
                        Completado = dto.Completado,
                        FechaCompletado = dto.Completado ? DateTime.Now : null,
                        FechaActualizacion = DateTime.Now
                    };

                    _context.PasosCompletados.Add(nuevoPaso);

                    _logger.LogInformation("Insertando nuevo PasoCompletado: {@Paso}", nuevoPaso);
                }
                else
                {
                    // Actualizar registro existente
                    pasoExistente.Completado = dto.Completado;
                    pasoExistente.PasoNombre = dto.PasoNombre;
                    pasoExistente.FechaCompletado = dto.Completado ? DateTime.Now : null;
                    pasoExistente.FechaActualizacion = DateTime.Now;

                    _logger.LogInformation("Actualizando PasoCompletado existente: {@Paso}", pasoExistente);
                }

                // Guardar cambios
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Mensaje = "Paso registrado exitosamente",
                    Completado = dto.Completado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar paso completado");
                return StatusCode(500, new { Mensaje = "Error interno del servidor" });
            }
        }


        [HttpGet("por-empresa/{empresaId}")]
        public async Task<IActionResult> ObtenerPasosCompletados(long empresaId)
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Verificar relación empresa-usuario
                var tieneAcceso = await _context.Empresas
                    .AnyAsync(e => e.ID == empresaId && e.UserId == userId);

                if (!tieneAcceso)
                {
                    return Unauthorized(new { Mensaje = "No tienes acceso a esta empresa" });
                }

                var pasos = await _context.PasosCompletados
                    .Where(p => p.EmpresaId == empresaId && p.UserId == userId)
                    .OrderBy(p => p.PasoId)
                    .Select(p => new {
                        p.PasoId,
                        p.PasoNombre,
                        p.Completado,
                        p.FechaCompletado,
                        PorcentajeCompletado = _context.PasosCompletados
                            .Count(pc => pc.EmpresaId == empresaId && pc.UserId == userId && pc.Completado) * 100 / 7 // Total de pasos
                    })
                    .ToListAsync();

                return Ok(pasos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pasos completados");
                return StatusCode(500, new { Mensaje = "Error interno del servidor" });
            }
        }

        public class RegistrarPasoCompletadoDto
        {
            [Required]
            public long EmpresaId { get; set; }
            [Required]
            public long UserId { get; set; }

            [Required]
            public int PasoId { get; set; }

            [Required]
            [StringLength(100)]
            public string PasoNombre { get; set; }

            [Required]
            public bool Completado { get; set; }
        }
    }
}
