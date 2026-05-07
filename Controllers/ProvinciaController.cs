using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using eCertify.Models;
using eCertify.Data;
using Microsoft.EntityFrameworkCore;
using eCertify.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace eCertify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProvinciaController : ControllerBase
    {
        private readonly SogeDbContext _context;

        public ProvinciaController(SogeDbContext context)
        {
            _context = context;
        }

        // GET: api/Provincia
        [HttpGet("ListarConMunicipios")]
        public async Task<ActionResult<IEnumerable<ProvinciaDTO>>> GetProvincias()
        {
            return await _context.Provincias
                .Include(p => p.Municipios)
                .Select(p => new ProvinciaDTO
                {
                    Id = p.Prov_Id,
                    Descripcion = p.Descripcion,
                    Municipios = p.Municipios.Select(m => new MunicipioDTO
                    {
                        Id = m.Muni_Id,
                        Descripcion = m.Descripcion
                    }).ToList()
                })
                .ToListAsync();
        }

        // GET: api/Provincia/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProvinciaDTO>> GetProvincia(int id)
        {
            var provincia = await _context.Provincias
                .Include(p => p.Municipios)
                .Where(p => p.Prov_Id == id)
                .Select(p => new ProvinciaDTO
                {
                    Id = p.Prov_Id,
                    Descripcion = p.Descripcion,
                    Municipios = p.Municipios.Select(m => new MunicipioDTO
                    {
                        Id = m.Muni_Id,
                        Descripcion = m.Descripcion
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (provincia == null)
            {
                return NotFound();
            }

            return provincia;
        }
    }
}

