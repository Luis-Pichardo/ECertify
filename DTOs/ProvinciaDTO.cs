namespace eCertify.DTOs
{
    public class ProvinciaDTO
    {
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public List<MunicipioDTO> Municipios { get; set; }
    }

}
