namespace eCertify.Models
{
    public class Simulacion
    {
        public string TipoECF { get; set; }
        public int SecuenciaECF { get; set; }
        public string? ClienteRNC { get; set; }
        public string? ClienteNombre { get; set; }
        public string ProductoNombre { get; set; }
        public decimal ProductoCantidad { get; set; } = 1;
        public decimal ProductoPrecio { get; set; }
        public string ProductoTipo { get; set; }
        public string UnidadeMedida { get; set; } 
        public decimal? ProductoItbis { get; set; } = 18;
        public decimal? RetencionISR { get; set; }
        public string? NCFModificado { get; set; }
        public string? RazonModificacion { get; set; }
    }
}
