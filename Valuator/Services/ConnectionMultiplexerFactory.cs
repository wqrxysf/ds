using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Valuator.Services
{
    public class ConnectionMultiplexerFactory
    {
        private readonly IOptions<RedisSettings> _redisSettings;

        public ConnectionMultiplexerFactory(IOptions<RedisSettings> redisSettings)
        {
            _redisSettings = redisSettings;
        }

        private static readonly Dictionary<string, string> countryMap = new Dictionary<string, string>
        {   
            { "MAIN", "localhost:6379" },
            { "RU", "localhost:6380" },
            { "EU", "localhost:6381" },
            { "ASIA", "localhost:6382" }
        };
        public IConnectionMultiplexer GetConnection(string region)
        {
            countryMap.TryGetValue(region, out string address);

            if (string.IsNullOrEmpty(address))
                throw new InvalidOperationException($"'{region}' не найдена");

            string password = _redisSettings.Value.Password;
            string connectionString = $"{address},password={password}";

            return ConnectionMultiplexer.Connect(connectionString);
        }
        public async Task<IConnectionMultiplexer> GetShardConnectionByTaskIdAsync(
            string taskId,
            IConnectionMultiplexer mainDb,
            ILogger logger)
        {
            var db = mainDb.GetDatabase();

            var regionValue = await db.StringGetAsync($"shardmap:{taskId}");

            if (regionValue.IsNullOrEmpty)
                throw new KeyNotFoundException($"Задача {taskId} не найдена");

            string region = regionValue.ToString();

            string logMessage = $"LOOKUP: {taskId}, {region}";
            logger.LogInformation(logMessage);
            Console.WriteLine(logMessage);

            return GetConnection(region);
        }
    }
}