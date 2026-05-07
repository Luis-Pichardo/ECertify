using eCertify.Models;

namespace eCertify.Interfaces
{
    public interface IHistorialPagoService
    {
        Task<HistorialPago?> RegistrarPagoAsync(HistorialPago pago);
        Task<IEnumerable<HistorialPago>> ObtenerPagosAsync(long userId, long empresaId);
    }
}
