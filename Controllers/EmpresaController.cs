
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eCertify.DTOs;
using eCertify.Interfaces;
using eCertify.Models;
using eCertify.Utils;
using System.Security.Claims;

namespace eCertify.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresaController : ControllerBase
    {
        private readonly IEmpresaService _empresaService;
        private readonly ILogger<EmpresaController> _logger;
        private readonly IFileStorageManager _fileStorageManager;

        public EmpresaController(IEmpresaService empresaService, ILogger<EmpresaController> logger, IFileStorageManager fileStorageManager)
        {
            _empresaService = empresaService;
            _logger = logger;
            _fileStorageManager = fileStorageManager;
        }

        [HttpGet("Listar")]
        public async Task<IActionResult> Listar()
        {
            try
            {
                _logger.LogInformation("Listando todas las empresas.");
                var empresas = await _empresaService.ListarEmpresasAsync();
                return Ok(empresas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar las empresas.");
                return StatusCode(500, "Error interno del servidor al listar las empresas.");
            }
        }

        [HttpGet("Buscar/{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                _logger.LogInformation("Buscando empresa con ID: {EmpresaId}", id);
                var empresa = await _empresaService.GetEmpresaByIdAsync(id);
                if (empresa == null)
                {
                    _logger.LogWarning("Empresa con ID {EmpresaId} no encontrada.", id);
                    return NotFound($"No se encontró una empresa con ID {id}.");
                }

                return Ok(empresa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar la empresa con ID: {EmpresaId}", id);
                return StatusCode(500, $"Error interno del servidor al buscar la empresa con ID {id}.");
            }
        }

        [HttpGet("ListarPorUsuario/{userId}")]
        public async Task<IActionResult> ListarPorUsuario(long userId)
        {
            try
            {
                var empresas = await _empresaService.ListarEmpresasPorUsuarioAsync(userId);
                return Ok(empresas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar empresas por usuario.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }


        [HttpPost("Agregar")]
        public async Task<IActionResult> CrearEmpresa([FromForm] EmpresaUploadDTO dto)
        {
            try
            {
                var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Usuario autenticado con NameIdentifier: {NameId}", nameId);

                _logger.LogInformation("Claims recibidos:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                                  User.FindFirst("nameid") ??
                                  User.FindFirst(ClaimTypes.Name);

                // Limpiar RNC
                if (string.IsNullOrWhiteSpace(dto.RNC))
                    return BadRequest("El RNC es requerido.");

                string rncLimpio = Utils.Utils.LimpiarRNC(dto.RNC);

                if (string.IsNullOrEmpty(rncLimpio))
                    return BadRequest("El RNC debe contener dígitos válidos.");

                // Verificar si ya existe empresa con ese RNC
                var empresaExistente = await _empresaService.GetEmpresaByRncAsync(rncLimpio);
                if (empresaExistente != null)
                {
                    return Conflict($"Ya existe una empresa registrada con el RNC {rncLimpio}.");
                }

                // Guardar archivos adjuntos si vienen
                string nombreCertificado = null;
                string nombreExcel = null;
                string nombreLogo = null;

                if (dto.Certificado != null)
                {
                    nombreCertificado = Path.GetFileName(dto.Certificado.FileName);
                    await _fileStorageManager.SaveFileAsync(rncLimpio, FileStorageManager.StorageType.Certificados, dto.Certificado, nombreCertificado);
                }

                if (dto.ExcelPruebas != null)
                {
                    nombreExcel = Path.GetFileName(dto.ExcelPruebas.FileName);
                    await _fileStorageManager.SaveFileAsync(rncLimpio, FileStorageManager.StorageType.PruebasExcel, dto.ExcelPruebas, nombreExcel);
                }

                if (dto.LogoArchivo != null)
                {
                    nombreLogo = Path.GetFileName(dto.LogoArchivo.FileName);
                    await _fileStorageManager.SaveFileAsync(rncLimpio, FileStorageManager.StorageType.Imagenes, dto.LogoArchivo, nombreLogo);
                }

                // Crear empresa
                var empresa = new Empresa
                {
                    RNC = rncLimpio,
                    RazonSocial = dto.RazonSocial,
                    NombreCertificadop12 = nombreCertificado,
                    NombreExcelPruebas = nombreExcel,
                    CertificadoPass = dto.CertificadoPass,
                    Logo = nombreLogo,
                    Created = DateTime.UtcNow,
                    Status = dto.Status ?? 1,
                    SuscripcionID = dto.SuscripcionID,
                    Direccion = dto.Direccion,
                    ProvinciaId = dto.ProvinciaId,
                    MunicipioId = dto.MunicipioId,
                    UserId = dto.UserId
                };

                var nuevaEmpresa = await _empresaService.CrearEmpresaAsync(empresa);

                return CreatedAtAction(nameof(GetById), new { id = nuevaEmpresa.ID }, nuevaEmpresa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empresa");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }



        [HttpPut("Actualizar/{id}")]
        public async Task<IActionResult> ActualizarEmpresa(long id, [FromBody] EmpresaUpdateDTO dto)
        {
            if (id != dto.ID)
                return BadRequest("El ID de la ruta no coincide con el ID del cuerpo.");

            var empresa = await _empresaService.GetEmpresaByIdAsync(id);
            if (empresa == null)
                return NotFound();

            // Solo actualizamos si el campo tiene valor
            if (dto.RNC is not null) empresa.RNC = dto.RNC;
            if (dto.RazonSocial is not null) empresa.RazonSocial = dto.RazonSocial;
            if (dto.NombreExcelPruebas is not null) empresa.NombreExcelPruebas = dto.NombreExcelPruebas;
            if (dto.NombreCertificadop12 is not null) empresa.NombreCertificadop12 = dto.NombreCertificadop12;
            if (dto.CertificadoPass is not null) empresa.CertificadoPass = dto.CertificadoPass;
            if (dto.Logo is not null) empresa.Logo = dto.Logo;
            if (dto.Status.HasValue) empresa.Status = dto.Status.Value;
            if (dto.Created.HasValue) empresa.Created = dto.Created.Value;
            if (dto.SuscripcionID.HasValue) empresa.SuscripcionID = dto.SuscripcionID.Value;
            if(dto.Direccion is not null) empresa.Direccion = dto.Direccion;
            if (dto.ProvinciaId.HasValue) empresa.ProvinciaId = dto.ProvinciaId.Value;
            if (dto.MunicipioId.HasValue) empresa.MunicipioId = dto.MunicipioId.Value;

            var actualizado = await _empresaService.ActualizarEmpresaAsync(empresa);

            if (!actualizado)
                return StatusCode(500, "No se pudo actualizar la empresa.");

            return Ok("Empresa actualizada correctamente.");

        }



        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> EliminarEmpresa(long id)
        {
            try
            {
                _logger.LogInformation("Eliminando empresa con ID: {EmpresaId}", id);
                var eliminado = await _empresaService.EliminarEmpresaAsync(id);
                if (!eliminado)
                {
                    _logger.LogWarning("Empresa con ID {EmpresaId} no encontrada para eliminar.", id);
                    return NotFound($"No se encontró una empresa con ID {id} para eliminar.");
                }

                _logger.LogInformation("Empresa con ID {EmpresaId} eliminada correctamente.", id);
                return Ok($"Empresa con ID {id} eliminada exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la empresa con ID: {EmpresaId}", id);
                return StatusCode(500, $"Error interno del servidor al eliminar la empresa con ID {id}.");
            }
        }
    }
}
