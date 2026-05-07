using System.Text.Json.Serialization;

namespace eCertify.DTOs
{
    public class FacturaResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UrlQR { get; set; }
        public string FechaHoraFirma { get; set; }
        public string CodigoSeguridad { get; set; }
        public string RncComprador { get; set; }
        public string RazonSocialComprador { get; set; }
        public DgiiResponseDto RespuestaDGII { get; set; }
    }

    public class DgiiResponseDto
    {
        [JsonPropertyName("trackId")]
        public string TrackIdDgii { get; set; }
        [JsonIgnore]
        public Guid TrackId => Guid.TryParse(TrackIdDgii, out var guid) ? guid : Guid.Empty;
        public string Codigo { get; set; }
        public string Estado { get; set; }
        public string Rnc { get; set; }

        [JsonPropertyName("encf")]
        public string ENCF { get; set; }

        [JsonPropertyName("secuenciaUtilizada")]
        public bool SecuenciaUtilizada { get; set; }

        [JsonPropertyName("fechaRecepcion")]
        public string FechaRecepcion { get; set; }

        [JsonPropertyName("mensajes")]
        public List<MensajeDto> Mensajes { get; set; } = new List<MensajeDto>();
    }

    public class MensajeDto
    {
        public string Valor { get; set; }
        public int Codigo { get; set; }
    }
}
