using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eCertify.Pages.Certificacion
{
    public class RequirementsModel : PageModel
    {
        private readonly ILogger<RequirementsModel> _logger;

        public RequirementsModel(ILogger<RequirementsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("[Requirements] Page accessed by {User}", User.Identity?.Name);
        }
    }
}
