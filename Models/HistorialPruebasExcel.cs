namespace eCertify.Models
{
    public class HistorialPruebasExcel
    {
        public int Id { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public string Tipo { get; set; }           // 'ECF' o 'RFCE'
        public string Rnc { get; set; }
        public string ArchivoXml { get; set; }
        public string Encf { get; set; }
        public string EstadoEnvio { get; set; }
        public string CodigoRespuesta { get; set; }
        public Guid? TrackId { get; set; }
        public string Mensajes { get; set; }
        public bool? SecuenciaUtilizada { get; set; }
    }
}
