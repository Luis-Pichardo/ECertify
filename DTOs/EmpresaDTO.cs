using System.Text.Json.Serialization;

namespace eCertify.DTOs
{
    public class EmpresaDTO
    {
        public string? RNC { get; set; }
        public string? RazonSocial { get; set; }
        public string? NombreExcelPruebas { get; set; }
        public string? NombreCertificadop12 { get; set; }
        public string? CertificadoPass { get; set; }
        public string? Logo { get; set; }
        public int? Status { get; set; } = 0;
        public DateTime? Created { get; set; } = DateTime.Now;
        public int? SuscripcionID { get; set; }

        public string? Direccion { get; set; }
        public int? ProvinciaId { get; set; }
        public int? MunicipioId { get; set; }
    }
}
