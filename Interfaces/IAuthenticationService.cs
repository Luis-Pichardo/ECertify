using eCertify.Models;

namespace eCertify.Interfaces
{
    public interface IAuthenticationService
    {
        string GenerateJwtToken(User user);
        Task<string> GetAuthToken();
    }
}