namespace eCertify.DTOs.Front
{
    public class PasosCompletadosDTO
    {
        public long EmpresaId { get; set; }
        public string PasoNombre { get; set; }
        public bool Completado { get; set; }
        public DateTime? FechaCompletado { get; set; }
    }
}
