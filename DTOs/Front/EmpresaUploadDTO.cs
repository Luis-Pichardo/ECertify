namespace eCertify.DTOs.Front
{
    public class EmpresaUploadDTO : EmpresaDTO
    {
        public IFormFile? LogoArchivo { get; set; }
        public IFormFile? Certificado { get; set; }
        public IFormFile? ExcelPruebas { get; set; }
    }
}
