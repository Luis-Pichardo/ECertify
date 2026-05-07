using System.Text.Json.Serialization;

namespace eCertify.DTOs.Front
{
    public class FacturaHistorialDTO
    {
        public string ENCF { get; set; }
        public string FechaRecepcion { get; set; }
        public string RazonSocialComprador { get; set; }
        public string RncComprador { get; set; }
        public string RncEmisor { get; set; }
        public string Estado { get; set; }
        public string CodigoSeguridad { get; set; }
        public string TrackId { get; set; }
        public string MensajeValor { get; set; }
        public int MensajeCodigo { get; set; }
    }
}
