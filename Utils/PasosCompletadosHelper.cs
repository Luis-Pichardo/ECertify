using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace eCertify.Utils
{
    public static class PasosCompletadosHelper
    {
        public static async Task<bool> RegistrarPasoCompletado(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ClaimsPrincipal user,
            string pasoNombre,
            ILogger logger,
            int pasoId)
        {
            try
            {
                logger.LogInformation("Iniciando registro del paso {PasoId}: {PasoNombre}", pasoId, pasoNombre);

                // 1. Validar usuario autenticado
                if (user?.Identity?.IsAuthenticated != true)
                {
                    logger.LogWarning("Usuario no autenticado");
                    return false;
                }

                // 2. Obtener datos del usuario
                var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(user);
                var userId = long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

                if (empresa.ID == 0 || userId == 0)
                {
                    logger.LogError("Datos de usuario incompletos");
                    return false;
                }

                // 3. Preparar solicitud HTTP
                using var httpClient = httpClientFactory.CreateClient("ApiClient");
                var apiUrl = $"{configuration["ApiUrl"]}api/HistorialPruebasExcel/RegistrarPasos";

                var requestData = new
                {
                    EmpresaId = empresa.ID,
                    UserId = userId,
                    PasoId = pasoId,
                    PasoNombre = pasoNombre,
                    Completado = true
                };

                // 4. Enviar solicitud
                var json = JsonSerializer.Serialize(requestData);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(apiUrl, content);

                // 5. Procesar respuesta
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("Error en la API. Status: {StatusCode}, Respuesta: {Error}",
                        response.StatusCode, errorContent);
                    return false;
                }

                logger.LogInformation("Paso {PasoId} registrado exitosamente", pasoId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado al registrar paso");
                return false;
            }
        }

        // --- Consultar si un paso ya está completado ---
        public static async Task<bool> EstaCompletado(
    IHttpClientFactory httpClientFactory,
    ClaimsPrincipal user,
    ILogger logger,
    string pasoNombre)
        {
            try
            {
                if (user?.Identity?.IsAuthenticated != true)
                {
                    logger.LogWarning("Usuario no autenticado");
                    return false;
                }

                var empresa = ClaimHelper.ObtenerEmpresaDesdeClaims(user);
                if (empresa == null || empresa.ID == 0)
                {
                    logger.LogWarning("Empresa no encontrada en claims");
                    return false;
                }

                using var client = httpClientFactory.CreateClient("ApiClient");

                // Llamada al API para obtener pasos de la empresa
                var response = await client.GetAsync($"api/HistorialPruebasExcel/por-empresa/{empresa.ID}");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("No se pudo obtener estado de pasos para empresa {EmpresaId}", empresa.ID);
                    return false;
                }

                // Intentar leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // Primero intentar deserializar como lista directa
                    var pasos = JsonSerializer.Deserialize<List<PasoDto>>(responseContent);
                    if (pasos != null)
                        return pasos.Any(p => p.Nombre.Equals(pasoNombre, StringComparison.OrdinalIgnoreCase) && p.Completado);
                }
                catch
                {
                    // Si falla, intentar deserializar como objeto con propiedad Pasos
                    try
                    {
                        var dataWrapper = JsonSerializer.Deserialize<PasoCompletadoDto>(responseContent);
                        return dataWrapper?.Pasos.Any(p => p.Nombre.Equals(pasoNombre, StringComparison.OrdinalIgnoreCase) && p.Completado) ?? false;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error al deserializar respuesta de API: {Content}", responseContent);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al consultar pasos completados para empresa");
                return false;
            }
        }
    }

    public class PasoCompletadoDto
    {
        public List<PasoDto> Pasos { get; set; } = new List<PasoDto>();
    }

    public class PasoDto
    {
        public int PasoId { get; set; }
        public string Nombre { get; set; } = "";
        public bool Completado { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public int PorcentajeCompletado { get; set; }
    }

}