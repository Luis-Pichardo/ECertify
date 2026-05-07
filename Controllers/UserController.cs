using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using eCertify.DTOs;
using eCertify.Interfaces;
using eCertify.Models;

namespace eCertify.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("Listar")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            _logger.LogInformation("Obteniendo todos los usuarios");
            var users = await _userService.GetAllUsersAsync();
            return Ok(new { message = "Usuarios obtenidos exitosamente", data = users });
        }

        [HttpGet("Buscar/{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(long id)
        {
            _logger.LogInformation("Buscando usuario con ID {UserId}", id);
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("Usuario con ID {UserId} no encontrado", id);
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(new { message = "Usuario encontrado exitosamente", data = user });
        }

        [HttpPost("Agregar")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> CreateUser([FromBody] User user)
        {
            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

                user.Password = hashedPassword;

                var createdUser = await _userService.CreateUserAsync(user);
                _logger.LogInformation("Usuario creado con ID {UserId}", createdUser);

                return CreatedAtAction(nameof(GetUser), new { id = createdUser }, new
                {
                    message = "Usuario creado exitosamente",
                    data = createdUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return StatusCode(500, new { message = "Error interno al crear el usuario" });
            }
        }

        [HttpPut("Actualizar/{id}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] User user)
        {
            try
            {
                // Obtener el usuario actual de la BD para conservar la contraseña actual si no se manda una nueva
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    _logger.LogWarning("Usuario con ID {UserId} no encontrado para actualizar", id);
                    return NotFound(new { message = "Usuario no encontrado para actualizar" });
                }

                // Conservar contraseña si no se manda nueva
                if (!string.IsNullOrWhiteSpace(user.Password))
                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                else
                    user.Password = existingUser.Password;

                var result = await _userService.UpdateUserAsync(id, user);
                if (!result)
                    return NotFound(new { message = "Usuario no encontrado para actualizar" });

                _logger.LogInformation("Usuario con ID {UserId} actualizado exitosamente", id);
                return Ok(new { message = "Usuario actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario con ID {UserId}", id);
                return StatusCode(500, new { message = "Error interno al actualizar el usuario" });
            }
        }

        //[HttpDelete("Eliminar/{id}")]
        //public async Task<IActionResult> DeleteUser(long id)
        //{
        //    try
        //    {
        //        var result = await _userService.DeleteUserAsync(id);
        //        if (!result)
        //        {
        //            _logger.LogWarning("No se pudo eliminar el usuario con ID {UserId}", id);
        //            return NotFound(new { message = "Usuario no encontrado para eliminar" });
        //        }

        //        _logger.LogInformation("Usuario con ID {UserId} eliminado exitosamente", id);
        //        return Ok(new { message = "Usuario eliminado exitosamente" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error al eliminar usuario con ID {UserId}", id);
        //        return StatusCode(500, new { message = "Error interno al eliminar el usuario" });
        //    }
        //}
    }
}
