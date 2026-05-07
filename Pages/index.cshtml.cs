using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Services.Front;

namespace eCertify.Pages
{
    public class IndexModel : PageModel
    {
    
        private readonly ILogger<IndexModel> _logger;
        private readonly EmpresaAuthService _empresaAuthService;

        public IndexModel(ILogger<IndexModel> logger, EmpresaAuthService empresaAuthService)
        {
            _logger = logger;
            _empresaAuthService = empresaAuthService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> OnGetAsync()
        {
            // Si el usuario ya est� autenticado con el esquema de Usuario
            var usuarioAuth = await HttpContext.AuthenticateAsync("UsuarioScheme");
            if (usuarioAuth.Succeeded && usuarioAuth.Principal.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Home");
            }

            // Si el usuario ya est� autenticado con el esquema de Empresa
            var empresaAuth = await HttpContext.AuthenticateAsync("EmpresaScheme");
            if (empresaAuth.Succeeded && empresaAuth.Principal.Identity.IsAuthenticated)
            {
                var rncClaim = empresaAuth.Principal.Claims.FirstOrDefault(c => c.Type == "Rnc")?.Value;

                if (!string.IsNullOrEmpty(rncClaim))
                {
                    return RedirectToPage("/ConsultarFacturas", new { rnc = rncClaim });
                }

                return RedirectToPage("/ConsultarFacturas");
            }

            // Si no est� autenticado, se queda en la p�gina de inicio
            return Page();
        }
    }
}
