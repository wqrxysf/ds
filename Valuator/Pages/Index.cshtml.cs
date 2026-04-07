using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

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

        string textKey = $"text:{id}";
        await _redis.StringSetAsync(textKey, text);

        var task = new
        {
            Id = id,
            Text = text
        };
        var messageJson = JsonSerializer.Serialize(task);

        var factory = new ConnectionFactory { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "valuator.processing.rank",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var body = Encoding.UTF8.GetBytes(messageJson);
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "valuator.processing.rank",
            mandatory: false,
            body: body
        );

        await _redis.StringSetAsync($"rank:{id}", "calculating");

        return Redirect($"summary?id={id}");
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
