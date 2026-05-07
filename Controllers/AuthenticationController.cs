using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eCertify.Models;
using eCertify.Data;
using Microsoft.AspNetCore.Authorization;
using eCertify.Interfaces;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Asegura que sea accesible sin autenticación
public class AuthenticationController : ControllerBase
{
    private readonly SogeDbContext _context;
    private readonly IAuthenticationService _authService;

    public AuthenticationController(SogeDbContext context, IAuthenticationService authenticationService)
    {
        _context = context;
        _authService = authenticationService;
    }

    [HttpPost("validate-login")]
    public async Task<IActionResult> ValidateLogin([FromBody] LoginRequest model)
    {
        try
        {
            var user = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                return Unauthorized("Credenciales inválidas");

            var token = _authService.GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.ID,
                    user.Email,
                    user.Name,
                    user.LastName
                }
            });
        }
        catch (Exception ex)
        {
            // Log interno — nunca exponer detalles al cliente
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthenticationController>>();
            logger.LogError(ex, "Error al validar credenciales para {Email}", model?.Email);
            return StatusCode(500, "Error interno del servidor");
        }
    }
}

