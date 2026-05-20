using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Valuator.Services
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        Task<bool> RegisterAsync(string login, string password);
        Task<User?> AuthenticateAsync(string login, string password);
        Task<User?> GetByIdAsync(string userId);
    }

    public class UserService : IUserService
    {
        private static readonly ConcurrentDictionary<string, User> _users = new();

        public Task<bool> RegisterAsync(string login, string password)
        {
            if (_users.Values.Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            var user = new User
            {
                Login = login,
                PasswordHash = hash,
                Salt = salt
            };

            _users.TryAdd(user.Id, user);
            return Task.FromResult(true);
        }

        public Task<User?> AuthenticateAsync(string login, string password)
        {
            var user = _users.Values.FirstOrDefault(u =>
                u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));

            if (user == null) return Task.FromResult<User?>(null);

            var hash = HashPassword(password, user.Salt);
            if (hash != user.PasswordHash) return Task.FromResult<User?>(null);

            return Task.FromResult<User?>(user);
        }

        public Task<User?> GetByIdAsync(string userId)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        private static string GenerateSalt()
        {
            var bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}