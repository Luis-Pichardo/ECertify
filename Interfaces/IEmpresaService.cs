using eCertify.Models;

namespace eCertify.Interfaces
{
    public interface IEmpresaService
    {
        Task<IEnumerable<Empresa>> ListarEmpresasAsync();
        Task<Empresa?> GetEmpresaByIdAsync(long id);
        Task<Empresa?> GetEmpresaByRncAsync(string rnc);
        Task<Empresa> CrearEmpresaAsync(Empresa empresa);
        Task<bool> ActualizarEmpresaAsync(Empresa empresa);
        Task<bool> EliminarEmpresaAsync(long id);
        Task<List<Empresa>> ListarEmpresasPorUsuarioAsync(long userId);


    }
}
