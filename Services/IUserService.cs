using UrbanDrive.Models;

namespace UrbanDrive.Services
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string email, string password);
        Task<bool> RegisterUserAsync(User user, string password);
        Task<bool> CreateDriverAsync(User user, Driver driver, string password);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<bool> VerifyEmailAsync(string email, string token);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(int id);
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateToken();
    }
}