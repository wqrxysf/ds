using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _mainDb;
    private readonly ConnectionMultiplexerFactory _shardFactory;
    private readonly IUserService _userService;
    private readonly IOptions<RabbitMQSettings> _rabbitSettings;

    public string ServerPort { get; set; } = "";

    private static readonly Dictionary<string, string> CountryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Russia", "RU" },
        { "France", "EU" },
        { "Germany", "EU" },
        { "UAE", "ASIA" },
        { "India", "ASIA" }
    };
    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer mainDb, ConnectionMultiplexerFactory shardFactory, IUserService userService, IOptions<RabbitMQSettings> rabbitSettings)
    {
        _logger = logger;
        _mainDb = mainDb.GetDatabase();

        _shardFactory = shardFactory;

        _userService = userService;

        _rabbitSettings = rabbitSettings;
    }

    public void OnGet()
    {
        ServerPort = HttpContext.Connection.LocalPort.ToString();
        _logger.LogInformation("Запрос на порту: {Port}", ServerPort);
    }

    public async Task<IActionResult> OnPost(string text, string country)
    {
        if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = "/Index" });
        }

        _logger.LogDebug($"Текст: {text}, Страна: {country}");

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(country))
            return RedirectToPage();

        if (!CountryMap.TryGetValue(country, out string region))
        {
            ModelState.AddModelError("", "Неизвестная страна");
            return Page();
        }

        string id = Guid.NewGuid().ToString();

        string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation($"Задача {id} для региона {region}");

        var shardConnection = _shardFactory.GetConnection(region);
        var shardDb = shardConnection.GetDatabase();

        double similarity = CalculateSimilarity(text, shardDb);

        await shardDb.StringSetAsync($"text:{id}", text);
        await shardDb.StringSetAsync($"similarity:{id}", similarity.ToString());
        await shardDb.StringSetAsync($"rank:{id}", "calculating");

        await shardDb.StringSetAsync($"author:{id}", userId);

        await PublishSimilarityCalculatedEvent(id, similarity);

        await _mainDb.StringSetAsync($"shardmap:{id}", region);

        await PublishRankTask(id, text, region);

        return Redirect($"Summary?id={id}");
    }

    private async Task PublishRankTask(string id, string text, string region)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitSettings.Value.Host,
            UserName = _rabbitSettings.Value.Username,
            Password = _rabbitSettings.Value.Password
        };
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
            Text = text,
            Region = region
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
        var factory = new ConnectionFactory
        {
            HostName = _rabbitSettings.Value.Host,
            UserName = _rabbitSettings.Value.Username,
            Password = _rabbitSettings.Value.Password
        };
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

    private double CalculateSimilarity(string text, IDatabase db)
    {
        var multiplexer = ((ConnectionMultiplexer)db.Multiplexer);
        var server = multiplexer.GetServer(multiplexer.GetEndPoints().First());

        var keys = server.Keys(pattern: "text:*");

        foreach (var key in keys)
        {
            var existingText = db.StringGet(key);
            if (!existingText.IsNullOrEmpty && existingText == text)
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