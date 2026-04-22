using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Console.WriteLine("Запущен EventsLogger\n");

var factory = new ConnectionFactory { HostName = "localhost" };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();

channel.ExchangeDeclare(
    exchange: "events.similarity.fanout",
    type: "fanout",
    durable: true
);

channel.ExchangeDeclare(
    exchange: "events.rank.fanout",
    type: "fanout",
    durable: true
);

var similarityQueue = channel.QueueDeclare(
    queue: "",
    durable: false,
    exclusive: true,
    autoDelete: true
).QueueName;

channel.QueueBind(
    queue: similarityQueue,
    exchange: "events.similarity.fanout",
    routingKey: ""
);

var similarityConsumer = new EventingBasicConsumer(channel);
similarityConsumer.Received += (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var evt = JsonSerializer.Deserialize<SimilarityCalculatedEvent>(message);
        Console.WriteLine("СОБЫТИЕ: SimilarityCalculated");
        Console.WriteLine($"id: {evt.Id}");
        Console.WriteLine($"Similarity: {evt.Similarity}");

        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message}");
    }
};

channel.BasicConsume(similarityQueue, false, similarityConsumer);
Console.WriteLine("Подписан на: events.similarity.calculated");

var rankQueue = channel.QueueDeclare(
    queue: "",
    durable: false,
    exclusive: true,
    autoDelete: true
).QueueName;

channel.QueueBind(
    queue: rankQueue,
    exchange: "events.rank.fanout",
    routingKey: ""
);

var rankConsumer = new EventingBasicConsumer(channel);
rankConsumer.Received += (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var evt = JsonSerializer.Deserialize<RankCalculatedEvent>(message);

        Console.WriteLine("СОБЫТИЕ: RankCalculated");
        Console.WriteLine($"id: {evt.Id}");
        Console.WriteLine($"Rank: {evt.Rank}");

        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message}");
    }
};

channel.BasicConsume(rankQueue, false, rankConsumer);
Console.WriteLine("Подписан на: events.rank.calculated");

Thread.Sleep(Timeout.Infinite);

public class SimilarityCalculatedEvent
{
    public string Id { get; set; } = string.Empty;
    public double Similarity { get; set; }
}

public class RankCalculatedEvent
{
    public string Id { get; set; } = string.Empty;
    public double Rank { get; set; }
}