using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        if (string.IsNullOrEmpty(text))
            return Redirect($"index");

        string id = Guid.NewGuid().ToString();

        string similarityKey = "SIMILARITY-" + id;
        double similarity = CalculateSimilarity(text);
        _redis.StringSet(similarityKey, similarity.ToString());

        string textKey = "TEXT-" + id;
        _redis.StringSet(textKey, text);

        string rankKey = "RANK-" + id;
        double rank = CalculateRank(text);
        _redis.StringSet(rankKey, rank.ToString());

        return Redirect($"summary?id={id}");
    }

    private double CalculateRank(string text)
    {

        int alphabeticCount = text.Count(c =>
            ( char.IsLetter(c)));

        return 1.0 - (double)alphabeticCount / text.Length;
    }
    private double CalculateSimilarity(string text)
    {
        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "TEXT-*");

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
