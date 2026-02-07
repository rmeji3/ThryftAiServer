using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;

namespace ThryftAiServer.Services.Discovery;

public class VisualSearchService(
    ProductEnrichmentService enrichmentService,
    AppDbContext dbContext,
    ILogger<VisualSearchService> logger)
{
    public async Task<List<FashionProduct>> SearchByImageAsync(byte[] imageBytes)
    {
        logger.LogInformation("Starting visual search analysis...");

        // 1. Convert Image to a "Searchable Vibe" using existing AI vision engine
        // We reuse the enricher but we only care about the descriptive properties
        var analysis = await enrichmentService.AnalyzeImageAsync(imageBytes, "visual_search_session");
        
        var searchQualities = new List<string> 
        { 
            analysis.ProductName ?? "", 
            analysis.Color ?? "", 
            analysis.Category ?? "",
            analysis.MasterCategory ?? ""
        };
        
        // Add individual tokens from the description to broaden the search
        if (!string.IsNullOrEmpty(analysis.Description))
        {
            var tokens = analysis.Description.Split(' ')
                .Where(t => t.Length > 4)
                .Take(10);
            searchQualities.AddRange(tokens);
        }

        logger.LogInformation("Visual Search Terms: {Terms}", string.Join(", ", searchQualities));

        // 2. Perform Keyword Search against our enriched database
        var inventory = await dbContext.FashionProducts.ToListAsync();

        var results = inventory
            .Select(p => {
                double score = 0;
                var pName = p.ProductName?.ToLower() ?? "";
                var pDesc = p.Description?.ToLower() ?? "";
                var pColor = p.Color?.ToLower() ?? "";

                foreach (var quality in searchQualities.Where(q => !string.IsNullOrEmpty(q)))
                {
                    var q = quality.ToLower();
                    if (pName.Contains(q)) score += 5.0;
                    if (pDesc.Contains(q)) score += 2.0;
                    if (pColor.Contains(q)) score += 3.0;
                }

                return new { Product = p, Score = score };
            })
            .Where(x => x.Score > 2.0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Product)
            .Take(10)
            .ToList();

        logger.LogInformation("Visual search returned {Count} matches.", results.Count);
        return results;
    }
}
