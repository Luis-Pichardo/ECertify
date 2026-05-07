using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace eCertify.Models
{
    public class User
    {
        [Key]
        [JsonIgnore]
        public long ID { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        public int AccessToken { get; set; }

        [JsonIgnore]
        public DateTime CreatedDate { get; set; }

        public int Status { get; set; } = 0;

        [JsonIgnore]
        public ICollection<Empresa> Empresas { get; set; } = new List<Empresa>();
    }
}
