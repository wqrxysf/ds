using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

Console.WriteLine("Запущен RankCalculator");

var factory = new ConnectionFactory { HostName = "localhost" };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();

channel.QueueDeclare(
    queue: "valuator.processing.rank",
    durable: true,
    exclusive: false,
    autoDelete: false
);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += async (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        var task = JsonSerializer.Deserialize<RankingTask>(message);

        if (task == null || string.IsNullOrEmpty(task.Region))
        {
            channel.BasicAck(ea.DeliveryTag, false);
            return;
        }

        string logMessage = $"LOOKUP: {task.Id}, {task.Region}";
        Console.WriteLine(logMessage);

        string envName = $"DB_{task.Region}";
        string connectionString = Environment.GetEnvironmentVariable(envVarName);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"'{envName}' не найдена");
        }

        using var redisConnection = ConnectionMultiplexer.Connect(connectionString);
        var redis = redisConnection.GetDatabase();

        TimeSpan interval = TimeSpan.FromSeconds(new Random().Next(2, 7));
        await Task.Delay(interval);

        double rank = CalculateRank(task.Text);

        string redisKey = $"rank:{task.Id}";
        string redisValue = rank.ToString();

        Console.WriteLine($"Запись в шард [{task.Region}] ({connectionString})");
        Console.WriteLine($"   Ключ: {redisKey}, Значение: {redisValue}");

        bool isSaved = await redis.StringSetAsync(redisKey, redisValue);

        if (isSaved)
        {
            Console.WriteLine($" Данные записаны в Redis ({task.Region})");
        }
        else
        {
            Console.WriteLine($" ОШИБКА: Не удалось записать в Redis ({task.Region})");
        }

        PublishRankCalculatedEvent(task.Id, rank, channel);

        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message}");
    }
};

channel.BasicConsume(
    queue: "valuator.processing.rank",
    autoAck: false,
    consumer: consumer
);

Thread.Sleep(Timeout.Infinite);

static void PublishRankCalculatedEvent(string id, double rank, IModel channel)
{
    channel.ExchangeDeclare(
        exchange: "events.rank.fanout",
        type: "fanout",
        durable: true
    );

    var evt = new RankCalculatedEvent
    {
        Id = id,
        Rank = rank
    };

    var messageJson = JsonSerializer.Serialize(evt);
    var body = Encoding.UTF8.GetBytes(messageJson);

    channel.BasicPublish(
        exchange: "events.rank.fanout",
        routingKey: "",
        mandatory: false,
        body: body
    );

    Console.WriteLine("Published RankCalculated");
}

static double CalculateRank(string text)
{

    int alphabeticCount = text.Count(c =>
        (char.IsLetter(c)));

    return 1.0 - (double)alphabeticCount / text.Length;
}

public class RankingTask
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Region { get; set; };
}

public class RankCalculatedEvent
{
    public string Id { get; set; } = string.Empty;
    public double Rank { get; set; }
}