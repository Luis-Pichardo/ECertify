using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.DTOs.Front;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace eCertify.Pages.Pagos
{
    public class PagoModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PagoModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public PagoDto Pago { get; set; }

        public async Task<IActionResult> OnPostProcesarPago()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Login");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return RedirectToPage("/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                // ✅ 1. Obtener usuario actual
                var userResponse = await client.GetAsync($"api/User/Buscar/{userId}");
                userResponse.EnsureSuccessStatusCode();

                var userJson = await userResponse.Content.ReadAsStringAsync();
                // Deserializar el wrapper
                var apiResult = JsonSerializer.Deserialize<ApiResponse<UserDto>>(
                    userJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var usuario = apiResult?.Data;

                if (usuario == null)
                    throw new Exception("No se pudo obtener el usuario.");

                // Construir objeto para PUT usando datos actuales + AccessToken actualizado
                var updateUsuario = new
                {
                    Name = usuario.Name,       
                    LastName = usuario.LastName, 
                    Email = usuario.Email,       
                    Password = "",               
                    AccessToken = usuario.AccessToken + Pago.Quantity,
                    Status = usuario.Status     
                };

                var updateJson = new StringContent(
                    JsonSerializer.Serialize(updateUsuario),
                    Encoding.UTF8,
                    "application/json"
                );
                var updateResponse = await client.PutAsync($"api/User/Actualizar/{userId}", updateJson);
                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error en PUT: {updateResponse.StatusCode} - {errorContent}");
                    throw new Exception($"Error al actualizar usuario: {errorContent}");
                }

                // ✅ 4. Registrar HistorialPago
                var pagoHistorial = new
                {
                    userId = userId,
                    payPalOrderId = Pago.PayPalOrderId,
                    transactionId = Pago.TransactionId,
                    amount = Pago.Amount,
                    currency = Pago.Currency,
                    status = "COMPLETED",
                    payerEmail = Pago.PayerEmail,
                    payerName = Pago.PayerName,
                    paymentDate = DateTime.UtcNow
                };

                var pagoJson = new StringContent(JsonSerializer.Serialize(pagoHistorial), Encoding.UTF8, "application/json");
                var pagoResponse = await client.PostAsync("api/HistorialPago", pagoJson);
                pagoResponse.EnsureSuccessStatusCode();

                return RedirectToPage("/Pagos/PagoExitoso", new { PayerName = Pago.PayerName, Amount = Pago.Amount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error procesando el pago: {ex.Message}");
                return Page();
            }
        }

        public class ApiResponse<T>
        {
            public string Message { get; set; }
            public T Data { get; set; }
        }

    }
}
