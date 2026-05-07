using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eCertify.Pages.Pagos
{
    public class PagoExitosoModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string PayerName { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal Amount { get; set; }

        public void OnGet()
        {
        }
    }
}
