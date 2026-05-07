using System.ComponentModel.DataAnnotations;

namespace eCertify.Models
{
    public class Municipio
    {
        [Key]
        public int Muni_Id { get; set; }
        public string Descripcion { get; set; }
        public int Prov_Id { get; set; }
        public Provincia Provincia { get; set; }
    }
}
