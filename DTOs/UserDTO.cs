using System.Text.Json.Serialization;

namespace eCertify.DTOs
{
    public class UserDTO
    {
        public string? Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Password { get; set; }
        public int AccessToken { get; set; }
    }

    public class UserLoginDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; } = string.Empty;

    }
}
