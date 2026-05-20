namespace Valuator.Services
{
    public class RedisSettings
    {
        public string Main { get; set; } = "localhost:6379";
        public string Password { get; set; } = string.Empty;
    }

    public class RedisShardsSettings
    {
        public string RU { get; set; } = "localhost:6380";
        public string EU { get; set; } = "localhost:6381";
        public string ASIA { get; set; } = "localhost:6382";
    }

    public class RabbitMqSettings
    {
        public string Host { get; set; } = "localhost";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}