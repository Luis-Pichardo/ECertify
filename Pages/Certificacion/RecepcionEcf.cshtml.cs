using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eCertify.Utils;

namespace eCertify.Pages.Certificacion
{
    public class RecepcionEcfModel : PageModel
    {
        private readonly PlanValidator _planValidator;

        public RecepcionEcfModel(PlanValidator planValidator)
        {
            _planValidator = planValidator;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var validacion = await _planValidator.VerificarPlanAsync();
            if (validacion != null)
                return validacion;

            return Page();
        }
    }
}
