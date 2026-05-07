using System.ComponentModel.DataAnnotations;

namespace eCertify.DTOs.Front
{
    public class LoginDTO
    {
        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
