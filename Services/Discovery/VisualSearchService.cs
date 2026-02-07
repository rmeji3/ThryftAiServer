using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;

namespace ThryftAiServer.Services.Discovery;

public class VisualSearchService(
    ProductEnrichmentService enrichmentService,
    AppDbContext AppDbContext
    )
{
    public async Task<List<FashionProduct>> SearchByImageAsync(byte[] imageBytes)
    {
        Console.WriteLine("Starting visual search analysis...");

        // convert image to a "Searchable Vibe" string using existing AI vision engine
        var analysis = await enrichmentService.AnalyzeImageAsync(imageBytes, "visual_search_session");
        
        var searchQualities = new List<string> 
        { 
            analysis.ProductName ?? "", 
            analysis.Color ?? "", 
            analysis.Category ?? "",
            analysis.MasterCategory ?? ""
        };
        
        // add individual tokens from the description to broaden the search
        if (!string.IsNullOrEmpty(analysis.Description))
        {
            var tokens = analysis.Description.Split(' ')
                .Where(t => t.Length > 4)
                .Take(10);
            searchQualities.AddRange(tokens);
        }

        Console.WriteLine($"Visual Search Terms: {string.Join(", ", searchQualities)}");

        // perform keyword search against our db
        var inventory = await AppDbContext.FashionProducts.ToListAsync();

        // rank items by how well they match the search qualities
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

        Console.WriteLine($"Visual search returned {results.Count} matches.");
        return results;
    }
}
