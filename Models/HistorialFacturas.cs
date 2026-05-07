namespace eCertify.Models
{
    public class HistorialFacturas
    {
        public int Id { get; set; }
        public string UrlQR { get; set; }
        public DateTime FechaHoraFirma { get; set; }
        public string CodigoSeguridad { get; set; }

        public Guid TrackId { get; set; }
        public string CodigoRespuesta { get; set; }
        public string Estado { get; set; }
        public string RncEmisor { get; set; }
        public string RncComprador { get; set; }
        public string RazonSocialComprador { get; set; }
        public string ENCF { get; set; }
        public bool SecuenciaUtilizada { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public string MensajeValor { get; set; }
        public int MensajeCodigo { get; set; }
    }
}
