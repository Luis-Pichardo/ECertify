using System.Security.Claims;
using eCertify.Models;

namespace eCertify.Utils
{
    public static class ClaimHelper
    {
        public static Empresa ObtenerEmpresaDesdeClaims(ClaimsPrincipal user)
        {
            return new Empresa
            {
                ID = long.Parse(user.FindFirst("EmpresaId")?.Value ?? "0"),
                RazonSocial = user.FindFirst("RazonSocial")?.Value ?? "",
                RNC = user.FindFirst("RNC")?.Value ?? "",
                Direccion = user.FindFirst("DireccionEmpresa")?.Value ?? "",
                Email = user.FindFirst(ClaimTypes.Email)?.Value ?? "",
                Logo = user.FindFirst("LogoEmpresa")?.Value ?? ""
            };
        }
    }
}
