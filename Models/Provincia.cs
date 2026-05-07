using System.ComponentModel.DataAnnotations;

namespace eCertify.Models
{
    public class Provincia
    {
        [Key]
        public int Prov_Id { get; set; }
        public string Descripcion { get; set; }
        public ICollection<Municipio> Municipios { get; set; }
    }
}
