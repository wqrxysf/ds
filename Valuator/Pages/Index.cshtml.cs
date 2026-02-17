using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();

        string similarityKey = "SIMILARITY-" + id;
        double similarity = CalculateSimilarity(text);  // посчитать similarity
        _redis.StringSet(similarityKey, similarity.ToString()); // сохранить в БД (Redis) по ключу similarityKey

        string textKey = "TEXT-" + id;
        _redis.StringSet(textKey, text); // сохранить в БД (Redis) text по ключу textKey

        string rankKey = "RANK-" + id;
        double rank = CalculateRank(text);  // посчитать rank
        _redis.StringSet(rankKey, rank.ToString()); // сохранить в БД (Redis) по ключу rankKey

        return Redirect($"summary?id={id}");
    }

    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int alphabeticCount = text.Count(c =>
            char.IsLetter(c) &&
            (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' ||
             c >= 'а' && c <= 'я' || c >= 'А' && c <= 'Я' ||
             c == 'ё' || c == 'Ё'));

        return 1.0 - (double)alphabeticCount / text.Length;
    }
    private double CalculateSimilarity(string text)
    {
        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "TEXT-*");

        foreach (var key in keys)
        {
            var existingText = _redis.StringGet(key);
            if (existingText == text && key != $"TEXT-{text}")
            {
                return 1.0;
            }
        }

        return 0.0;
    }
}
