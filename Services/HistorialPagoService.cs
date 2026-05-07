using eCertify.Models;
using eCertify.Data;
using Microsoft.EntityFrameworkCore;
using eCertify.Interfaces;

namespace eCertify.Services
{
    public class HistorialPagoService : IHistorialPagoService
    {
        private readonly SogeDbContext _context;
        private readonly ILogger<HistorialPagoService> _logger;

        public HistorialPagoService(SogeDbContext context, ILogger<HistorialPagoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HistorialPago?> RegistrarPagoAsync(HistorialPago pago)
        {
            try
            {
                _context.HistorialPagos.Add(pago);
                await _context.SaveChangesAsync();
                return pago;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar pago. UserId: {UserId}", pago.UserId);
                return null;
            }
        }

        public async Task<IEnumerable<HistorialPago>> ObtenerPagosAsync(long userId, long empresaId)
        {
            try
            {
                return await _context.HistorialPagos
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos. UserId: {UserId}", userId);
                return new List<HistorialPago>();
            }
        }
    }
}
