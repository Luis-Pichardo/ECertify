namespace eCertify.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public long UserId { get; set; }
    }
}
