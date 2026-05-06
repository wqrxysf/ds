using StackExchange.Redis;

namespace Valuator.Services
{
    public class ConnectionMultiplexerFactory
    {
        public IConnectionMultiplexer GetConnection(string region)
        {
            string envName = $"DB_{region}";
            string? connectionString = Environment.GetEnvironmentVariable(envName);

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException($"'{envName}' не найдена");

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