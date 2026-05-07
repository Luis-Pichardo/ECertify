using System.Text.Json.Serialization;

namespace eCertify.DTOs.Front
{
    public class AprobacionDTO
    {
        [JsonPropertyName("numeroFila")]
        public int NumeroFila { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("rncEmisor")]
        public string RncEmisor { get; set; }

        [JsonPropertyName("enCf")]
        public string ENCf { get; set; }

        [JsonPropertyName("fechaEmision")]
        public string FechaEmision { get; set; }

        [JsonPropertyName("montoTotal")]
        public decimal MontoTotal { get; set; }

        [JsonPropertyName("rncComprador")]
        public string RncComprador { get; set; }

        [JsonPropertyName("estado")]
        public int Estado { get; set; }

        [JsonPropertyName("detalleMotivoRechazo")]
        public string DetalleMotivoRechazo { get; set; }

        [JsonPropertyName("fechaHoraAprobacionComercial")]
        public string FechaHoraAprobacionComercial { get; set; }
    }
}
