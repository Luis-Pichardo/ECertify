using eCertify.Pages;
using System.Security.Claims;

namespace eCertify.Services.Front
{
    public interface IPasosCompletadosService
    {
        Task<List<CertificationStepViewModel>> ObtenerPasosAsync(ClaimsPrincipal user);
    }
}
