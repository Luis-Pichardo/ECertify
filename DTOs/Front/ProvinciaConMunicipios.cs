namespace eCertify.DTOs.Front
{
    public class ProvinciaConMunicipios
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public List<MunicipioDTO> Municipios { get; set; } = new();
    }

    public class MunicipioDTO
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

}
