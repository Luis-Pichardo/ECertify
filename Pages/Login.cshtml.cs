using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using eCertify.Services.Front;

public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly EmpresaAuthService _empresaAuthService;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public string Email { get; set; }

    [BindProperty]
    public string Password { get; set; }

    public LoginModel(AuthService authService, ILogger<LoginModel> logger, EmpresaAuthService empresaAuthService)
    {
        _authService = authService;
        _logger = logger;
        _empresaAuthService = empresaAuthService;
    }

    public void OnGet()
    {
        // Limpiar mensajes de error al cargar la página
        if (TempData["ErrorMessage"] != null)
        {
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        // Intenta login como usuario normal
        var (success, error) = await _authService.LoginAsync(Email, Password);

        if (success)
        {
            _logger.LogInformation("Usuario {Email} autenticado", Email);
            return RedirectToPage("/CompanySelect");
        }

        // Si falla, intenta login como empresa
        var (successEmpresa, errorEmpresa, rnc) = await _empresaAuthService.LoginEmpresaAsync(Email, Password);

        if (successEmpresa)
        {
            _logger.LogInformation("Empresa autenticada: {Email}", Email);
            return RedirectToPage("/ConsultarFacturas", new { rnc = rnc });
        }

        // Si ambos fallan, muestra mensaje de error
        ViewData["ErrorMessage"] = error ?? "Credenciales inválidas";
        return Page();
    }
}