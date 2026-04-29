using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _redis;
    public string ServerPort { get; set; } = "";

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
    }
    //private readonly ILogger<IndexModel> _logger;
    //private readonly IConnectionMultiplexer _mainDb;

    //private readonly ConnectionMultiplexerFactory _shardFactory;
    //public string ServerPort { get; set; } = "";

    //public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer mainDb)
    //{
    //    _logger = logger;
    //    _mainDb = mainDb;

    //    _shardFactory = new ConnectionMultiplexerFactory();
    //}

    public void OnGet()
    {
        ServerPort = HttpContext.Connection.LocalPort.ToString();

        _logger.LogInformation("Запрос на порту: {Port}", ServerPort);
    }

    public async Task<IActionResult> OnPost(string text)
    {
        _logger.LogDebug(text);

        if (string.IsNullOrEmpty(text))
            return Redirect($"index");

        string id = Guid.NewGuid().ToString();

        string similarityKey = $"similarity:{id}";
        double similarity = CalculateSimilarity(text);
        await _redis.StringSetAsync(similarityKey, similarity.ToString());

        await PublishSimilarityCalculatedEvent(id, similarity);

        string textKey = $"text:{id}";
        await _redis.StringSetAsync(textKey, text);

        await _redis.StringSetAsync($"rank:{id}", "calculating");

        await PublishRankTask(id, text);

        return Redirect($"summary?id={id}");
    }

    private async Task PublishRankTask(string id, string text)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "valuator.processing.rank",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var task = new
        {
            Id = id,
            Text = text
        };

        var messageJson = JsonSerializer.Serialize(task);
        var body = Encoding.UTF8.GetBytes(messageJson);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "valuator.processing.rank",
            mandatory: false,
            body: body
        );
    }

    private async Task PublishSimilarityCalculatedEvent(string id, double similarity)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "events.similarity.fanout",
            type: "fanout",
            durable: true
            );

        var evt = new SimilarityCalculatedEvent
        {
            Id = id,
            Similarity = similarity,
        };

        var messageJson = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(messageJson);

        await channel.BasicPublishAsync(
            exchange: "events.similarity.fanout",
            routingKey: "",
            mandatory: false,
            body: body
        );
    }
    private double CalculateSimilarity(string text)
    {
        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "text:*");

        foreach (var key in keys)
        {
            var existingText = _redis.StringGet(key);
            if (existingText == text)
            {
                return 1.0;
            }
        }

        return 0.0;
    }
}
public class SimilarityCalculatedEvent
{
    public string Id { get; set; } = string.Empty;
    public double Similarity { get; set; }
}