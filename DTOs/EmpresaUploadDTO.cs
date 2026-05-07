namespace eCertify.DTOs
{
    public class EmpresaUploadDTO : EmpresaDTO
    {
        public long UserId { get; set; }

        public IFormFile? LogoArchivo { get; set; }
        public IFormFile? Certificado { get; set; }
        public IFormFile? ExcelPruebas { get; set; }

    }
}
