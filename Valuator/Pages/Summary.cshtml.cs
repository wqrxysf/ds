using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Services;


namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IConnectionMultiplexer _mainDb;
    private readonly ConnectionMultiplexerFactory _shardFactory;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer mainDb, ConnectionMultiplexerFactory shardFactory)
    {
        _logger = logger;
        _mainDb = mainDb;
        _shardFactory = shardFactory;
    }

    public string? TaskId { get; set; }
    public double Rank { get; set; }
    public double Similarity { get; set; }
    public bool IsCompleted { get; private set; }

    public async Task<IActionResult> OnGet(string id)
    {
        _logger.LogDebug(id);

        TaskId = id;

        try
        {
            using var shardConnection = await _shardFactory.GetShardConnectionByTaskIdAsync(id, _mainDb, _logger);

            var shardDb = shardConnection.GetDatabase();

            string rankKey = $"rank:{id}";
            string similarityKey = $"similarity:{id}";

            var rankValue = await shardDb.StringGetAsync(rankKey);
            var similarityValue = await shardDb.StringGetAsync(similarityKey);

            if (rankValue.IsNullOrEmpty || rankValue == "calculating")
            {
                IsCompleted = false;
                Rank = 0;
            }
            else
            {
                IsCompleted = true;

                if (double.TryParse(rankValue, out double r))
                {
                    Rank = r;
                }
                else
                {
                    Rank = 0;
                    IsCompleted = false;
                }
            }

            if (!similarityValue.IsNullOrEmpty)
            {
                if (double.TryParse(similarityValue, out double s))
                {
                    Similarity = s;
                }
            }
            else
            {
                Similarity = 0;
            }
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, $"{id} не найден в Shard Map");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка id: {id}");
        }
        return Page();
    }
}
