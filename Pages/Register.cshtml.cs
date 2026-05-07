using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.DTOs.Front;
using eCertify.Services.Front;

namespace eCertify.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> _logger;
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;

        private const string attemptsKey = "AccessKeyAttempts";
        private const int maxAttempts = 3;

        public RegisterModel(
            ILogger<RegisterModel> logger,
            IHttpClientFactory httpClientFactory,
            AuthService authService,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _authService = authService;
            _configuration = configuration;
        }

        [BindProperty] public RegisterDTO input { get; set; } = new();
        [BindProperty] public string? accessKey { get; set; }

        public bool isBlocked { get; private set; }
        public int attemptsFailed { get; private set; }
        public bool showBlockWarn { get; private set; }
        public string? accessKeyError { get; private set; }

        public void OnGet()
        {
            attemptsFailed = HttpContext.Session.GetInt32(attemptsKey) ?? 0;
            isBlocked = attemptsFailed >= maxAttempts;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            attemptsFailed = HttpContext.Session.GetInt32(attemptsKey) ?? 0;

            if (IsSessionBlocked())
                return Page();

            if (!ValidateAccessKey())
                return Page();

            HttpContext.Session.SetInt32(attemptsKey, 0);

            if (!ModelState.IsValid)
                return Page();

            return await RegisterUserAsync();
        }

        private bool IsSessionBlocked()
        {
            if (attemptsFailed < maxAttempts)
                return false;

            _logger.LogWarning("[Register] POST attempted on a blocked session.");
            isBlocked = true;
            return true;
        }

        private bool ValidateAccessKey()
        {
            var correctKey = _configuration["ClaveAccesoEmpresaProd"];

            if (accessKey == correctKey)
                return true;

            attemptsFailed++;
            HttpContext.Session.SetInt32(attemptsKey, attemptsFailed);
            _logger.LogWarning("[Register] Wrong access key. Attempt {Current}/{Max}", attemptsFailed, maxAttempts);

            if (attemptsFailed >= maxAttempts)
            {
                _logger.LogWarning("[Register] Session blocked after {Max} failed attempts.", maxAttempts);
                isBlocked = true;
                return false;
            }

            showBlockWarn = attemptsFailed == maxAttempts - 1;
            accessKeyError = "Clave de acceso incorrecta.";
            return false;
        }

        private async Task<IActionResult> RegisterUserAsync()
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/User/Agregar", input);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[Register] User registration failed. Status: {Status} Body: {Body}",
                        (int)response.StatusCode, errorBody);
                    ModelState.AddModelError(string.Empty, "No se pudo completar el registro. Intenta nuevamente.");
                    return Page();
                }

                _logger.LogInformation("[Register] User registered successfully: {Email}", input.Email);

                var (loginSuccess, loginError) = await _authService.LoginAsync(input.Email, input.Password);

                if (loginSuccess)
                    return RedirectToPage("/CompanySelect");

                _logger.LogWarning("[Register] Auto-login failed after registration: {Error}", loginError);
                TempData["ErrorMessage"] = "Cuenta creada correctamente. Por favor, inicia sesión.";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Register] Unexpected error during user registration.");
                ModelState.AddModelError(string.Empty, "Error inesperado. Por favor, intenta más tarde.");
                return Page();
            }
        }
    }
}
