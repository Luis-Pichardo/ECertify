using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;

namespace eCertify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MunicipioController : ControllerBase
    {
        private readonly SogeDbContext _context;

        public MunicipioController(SogeDbContext context)
        {
            _context = context;
        }

        // GET: api/municipio/by-provincia/5
        [HttpGet("by-provincia/{provinciaId}")]
        public async Task<IActionResult> GetMunicipiosByProvincia(int provinciaId)
        {
            var municipios = await _context.Municipios
                .Where(m => m.Prov_Id == provinciaId)
                .Select(m => new
                {
                    id = m.Muni_Id,
                    nombre = m.Descripcion
                })
                .ToListAsync();

            return Ok(municipios);
        }
    }
}
