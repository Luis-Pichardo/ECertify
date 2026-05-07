using eCertify.DTOs;
using eCertify.Models;

namespace eCertify.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();
        Task<UserDTO> GetUserByIdAsync(long id);
        Task<UserDTO> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(long id, User user);
        Task<bool> DeleteUserAsync(long id);
    }
}
