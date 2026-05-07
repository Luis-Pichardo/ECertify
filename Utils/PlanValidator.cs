using Microsoft.AspNetCore.Mvc;
using eCertify.Services.Front;

namespace eCertify.Utils
{
    public class PlanValidator
    {
        private readonly UserInfoService _userService;

        public PlanValidator(UserInfoService userInfoService)
        {
            _userService = userInfoService;
        }

        public async Task<IActionResult?> VerificarPlanAsync()
        {
            var usuario = await _userService.ObtenerUsuarioActualAsync();

            if (usuario == null)
                return new RedirectToPageResult("/Login");

            if (usuario.AccessToken == 0 && usuario.EmpresaCount == 1)
                return new RedirectToPageResult("/PlanGratuito");

            return null;
        }
    }
}
