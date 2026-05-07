using eCertify.Interfaces;
using eCertify.Models;
using eCertify.Data;
using Microsoft.EntityFrameworkCore;
using eCertify.DTOs;

namespace eCertify.Services
{
    public class HistorialPruebasExcelService : IHistorialPruebasExcelService
    {
        private readonly SogeDbContext _context;

        public HistorialPruebasExcelService(SogeDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarEnvioAsync(HistorialPruebasExcel historial)
        {
            historial.FechaEnvio = DateTime.UtcNow;
            _context.HistorialPruebasExcel.Add(historial);
            await _context.SaveChangesAsync();
        }

        public async Task<HistorialPruebasExcel?> ObtenerHistorialPorIdAsync(int id)
        {
            return await _context.HistorialPruebasExcel.FindAsync(id);
        }

        public async Task<List<HistorialPruebasExcel>> ObtenerTodosAsync()
        {
            return await _context.HistorialPruebasExcel.ToListAsync();
        }



    }
}
