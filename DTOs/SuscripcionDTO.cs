namespace eCertify.DTOs
{
    public class SuscripcionDTO
    {
        public int ID { get; set; }
        public string Descripcion { get; set; }
        public int CantFacturas { get; set; }
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }

        // Relación con Empresas (si se necesita devolver información de Empresas)
        public ICollection<EmpresaDTO> Empresas { get; set; }
    }
}
