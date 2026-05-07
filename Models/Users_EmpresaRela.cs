namespace eCertify.Models
{
    public class Users_EmpresaRela
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public long EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }
    }
}
