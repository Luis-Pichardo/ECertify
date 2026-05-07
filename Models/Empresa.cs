using System.ComponentModel.DataAnnotations.Schema;

namespace eCertify.Models
{
    public class Empresa
    {
        public long ID { get; set; }
        public string? RNC { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreExcelPruebas { get; set; }
        public string? NombreCertificadop12 { get; set; }
        public string? CertificadoPass { get; set; }
        public string? Logo { get; set; }
        public int Status { get; set; }
        public DateTime? Created { get; set; }
        public int? SuscripcionID { get; set; }
        public string? Direccion { get; set; }
        [NotMapped]
        public string? Email { get; set; }
        public int? ProvinciaId { get; set; }
        public int? MunicipioId { get; set; }
        public long UserId { get; set; }
    }
}
