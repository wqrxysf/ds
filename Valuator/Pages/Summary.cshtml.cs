using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        // проинициализировать свойства Rank и Similarity значениями из БД (Redis)
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        var rankValue = _redis.StringGet(rankKey);
        var similarityValue = _redis.StringGet(similarityKey);

        if (!string.IsNullOrEmpty(rankValue))
        {
            Rank = double.Parse(rankValue);
        }

        if (!string.IsNullOrEmpty(similarityValue))
        {
            Similarity = double.Parse(similarityValue);
        }
    }
}
