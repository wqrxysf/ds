using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

Console.WriteLine("RankCalculator запущен");

var redis = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();

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

consumer.Received += (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        var task = JsonSerializer.Deserialize<RankingTask>(message);

        Thread.Sleep(3000);

        double rank = CalculateRank(task.Text);

        redis.StringSet($"rank:{task.Id}", rank.ToString());

        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ОШИБКА: {ex.Message}");
    }
};

channel.BasicConsume(
    queue: "valuator.processing.rank",
    autoAck: false,
    consumer: consumer
);

Thread.Sleep(Timeout.Infinite);

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
}