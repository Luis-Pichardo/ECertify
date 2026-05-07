namespace eCertify.Models
{
    public class Suscripcion
    {
        public int ID { get; set; } 
        public string Descripcion { get; set; } 
        public int CantFacturas { get; set; } 
        public int Status { get; set; }   
        public DateTime CreatedDate { get; set; } 

        // Propiedad de navegación
        public ICollection<Empresa> Empresas { get; set; }
    }
}
