using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using eCertify.DTOs;
using eCertify.Interfaces;
using eCertify.Models;

namespace eCertify.Services
{
    public class UserService : IUserService
    {
        private readonly SogeDbContext _context;

        public UserService(SogeDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            return await _context.AppUsers
                .Select(u => new UserDTO
                {
                    Email = u.Email,
                    CreatedDate = u.CreatedDate
                }).ToListAsync();
        }

        public async Task<UserDTO> GetUserByIdAsync(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return null;

            return new UserDTO
            {
                Email = user.Email,
                CreatedDate = user.CreatedDate,
                AccessToken = user.AccessToken
            };
        }

        public async Task<UserDTO> CreateUserAsync(User user)
        {
            try
            {
                user.CreatedDate = DateTime.UtcNow;
                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();

                return new UserDTO
                {
                    Email = user.Email,
                    CreatedDate = user.CreatedDate
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateUserAsync] ERROR: {ex.Message}");
                throw; // Re-lanza para que lo capture el controlador
            }
        }

        public async Task<bool> UpdateUserAsync(long id, User user)
        {
            var existingUser = await _context.AppUsers.FindAsync(id);
            if (existingUser == null) return false;

            // Solo actualizar campos que no sean nulos o vacíos
            if (!string.IsNullOrWhiteSpace(user.Email))
                existingUser.Email = user.Email;

            if (!string.IsNullOrWhiteSpace(user.Password))
                existingUser.Password = user.Password; // ya viene hasheada si se envía

            // AccessToken es numérico, así que solo actualizar si viene distinto de 0 o null
            // si AccessToken es int y nunca null, entonces siempre lo puedes actualizar
            existingUser.AccessToken = user.AccessToken;

            _context.AppUsers.Update(existingUser);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteUserAsync(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return false;

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
