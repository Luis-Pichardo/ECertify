using eCertify.Models;
using static eCertify.Services.HistorialPruebasExcelService;

namespace eCertify.Interfaces
{
    public interface IHistorialPruebasExcelService
    {
        Task RegistrarEnvioAsync(HistorialPruebasExcel historial);
        Task<HistorialPruebasExcel?> ObtenerHistorialPorIdAsync(int id);
        Task<List<HistorialPruebasExcel>> ObtenerTodosAsync();
    }
}
