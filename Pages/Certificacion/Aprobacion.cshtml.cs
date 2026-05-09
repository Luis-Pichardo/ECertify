using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eCertify.Pages.Certificacion
{
    public class AprobacionModel : PageModel
    {
        public IActionResult OnGet() =>
            RedirectToPagePermanent("/Certificacion/CommercialApproval");
    }
}
