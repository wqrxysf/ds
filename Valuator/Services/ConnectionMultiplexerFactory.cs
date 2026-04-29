using StackExchange.Redis;

namespace Valuator.Services
{
    public class ConnectionMultiplexerFactory
    {
        public IConnectionMultiplexer GetConnection(string region)
        {
            string envVarName = $"DB_{region}";

            string connectionString = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"Ошибка: Переменная окружения '{envVarName}' не найдена. " +
                    $"Запустите скрипт настройки или задайте её вручную.");
            }

            return ConnectionMultiplexer.Connect(connectionString);
        }
    }
}