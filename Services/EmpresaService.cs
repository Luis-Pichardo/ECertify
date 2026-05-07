using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using eCertify.Interfaces;
using eCertify.Models;

namespace eCertify.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly SogeDbContext _context;

        public EmpresaService(SogeDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Empresa>> ListarEmpresasAsync()
        {
            return await _context.Empresas.ToListAsync();
        }

        public async Task<Empresa?> GetEmpresaByIdAsync(long id)
        {
            return await _context.Empresas.FindAsync(id);
        }

        public async Task<Empresa?> GetEmpresaByRncAsync(string rnc)
        {
            return await _context.Empresas
                .FirstOrDefaultAsync(e => e.RNC == rnc);
        }

        public async Task<Empresa> CrearEmpresaAsync(Empresa empresa)
        {
            empresa.Created = DateTime.Now;
            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();
            return empresa;
        }

        public async Task<bool> ActualizarEmpresaAsync(Empresa empresa)
        {
            var existente = await _context.Empresas.FindAsync(empresa.ID);
            if (existente == null) return false;

            _context.Entry(existente).CurrentValues.SetValues(empresa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarEmpresaAsync(long id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return false;

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Empresa>> ListarEmpresasPorUsuarioAsync(long userId)
        {
            return await _context.Empresas
                .Where(e => e.UserId == userId)
                .ToListAsync();
        }


    }
}
