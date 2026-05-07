using eCertify.Pages;
using System.Security.Claims;

namespace eCertify.Services.Front
{
    public interface IPasosCompletadosService
    {
        Task<List<PasoViewModel>> ObtenerPasosAsync(ClaimsPrincipal user);
    }
}
