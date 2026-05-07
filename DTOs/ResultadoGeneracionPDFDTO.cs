namespace eCertify.DTOs
{
    public class ResultadoGeneracionPDFDTO
    {
        public string NombreArchivo { get; set; }
        public string PdfUrl { get; set; }
        public string QrUrl { get; set; }
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
    }
}
