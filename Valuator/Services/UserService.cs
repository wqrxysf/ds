using StackExchange.Redis;
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
        private readonly IDatabase _db;

        public UserService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<bool> RegisterAsync(string login, string password)
        {
            var userId = await _db.StringGetAsync($"user:login:{login.ToLower()}");
            if (!userId.IsNullOrEmpty)
            {
                return false;
            }


            var id = Guid.NewGuid().ToString();
            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            await _db.HashSetAsync($"user:{id}", new HashEntry[]
            {
                new HashEntry("login", login),
                new HashEntry("passwordHash", hash),
                new HashEntry("salt", salt)
            });

            await _db.StringSetAsync($"user:login:{login.ToLower()}", id);

            return true;

        }

        public async Task<User?> AuthenticateAsync(string login, string password)
        {
            var userId = await _db.StringGetAsync($"user:login:{login.ToLower()}");
            if (userId.IsNullOrEmpty) return null;

            var values = await _db.HashGetAllAsync($"user:{userId}");
            if (values.Length == 0) return null;

            var dict = values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

            if (!dict.TryGetValue("passwordHash", out var storedHash) ||
                !dict.TryGetValue("salt", out var salt))
                return null;

            if (HashPassword(password, salt) != storedHash) return null;

            return new User
            {
                Id = userId,
                Login = dict["login"],
                PasswordHash = storedHash,
                Salt = salt
            };
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            var values = await _db.HashGetAllAsync($"user:{userId}");
            if (values.Length == 0) return null;

            var dict = values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
            return new User
            {
                Id = userId,
                Login = dict["login"],
                PasswordHash = dict["passwordHash"],
                Salt = dict["salt"]
            };
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