namespace eCertify.DTOs
{
    public class FirmarXmlRequestDTO
    {
        public string RNC { get; set; }
        public IFormFile XmlArchivo { get; set; }
    }
}
