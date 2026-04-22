using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;


namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _redis;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
    }

    public string? TaskId { get; set; }
    public double Rank { get; set; }
    public double Similarity { get; set; }
    public bool IsCompleted { get; private set; }

    public IActionResult OnGet(string id)
    {
        _logger.LogDebug(id);

        TaskId = id;

        if (string.IsNullOrEmpty(id))
        {
            return RedirectToPage("/Index");
        }

        string rankKey = $"rank:{id}";
        string similarityKey = $"similarity:{id}";

        var rankValue = _redis.StringGet(rankKey);

        var similarityValue = _redis.StringGet(similarityKey);

        if (rankValue.IsNullOrEmpty || rankValue == "calculating")
        {
            IsCompleted = false;
            Rank = 0;
        }
        else
        {
            IsCompleted = true;
            Rank = double.Parse(rankValue);
        }

        if (!similarityValue.IsNullOrEmpty)
        {
            Similarity = double.Parse(similarityValue);
        }
        else
        {
            Similarity = 0; 
        }
        return Page();
    }
}
