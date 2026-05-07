namespace eCertify.DTOs.Front
{
    public class UserDto
    {
        public long Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int AccessToken { get; set; }
        public int Status { get; set; }
    }
}
