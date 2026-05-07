using System.ComponentModel.DataAnnotations.Schema;

namespace eCertify.Models
{
    public class PasoCompletado
    {
        public long ID { get; set; }

        [ForeignKey("Empresa")]
        public long EmpresaId { get; set; }
        public virtual Empresa Empresa { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public virtual User User { get; set; }

        public int PasoId { get; set; }
        public string PasoNombre { get; set; }
        public bool Completado { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
