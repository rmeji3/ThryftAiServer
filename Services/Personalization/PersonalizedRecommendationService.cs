using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;

namespace ThryftAiServer.Services.Personalization;

public class RecommendationAnalysis
{
    public string StyleSummary { get; set; } = string.Empty;
    public List<string> RecommendedQualities { get; set; } = new();
    public List<int> SeedProductIds { get; set; } = new();
}

public class PersonalizedRecommendationService(
    AppDbContext dbContext,
    Kernel kernel,
    ILogger<PersonalizedRecommendationService> logger)
{
    public async Task<List<FashionProduct>> GetPersonalizedRecommendationsAsync(string userId = "global-user")
    {
        // 1. Fetch user's purchase history (with product details)
        var purchases = await dbContext.Purchases
            .Include(p => p.Product)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(5) // Look at 5 most recent purchases
            .ToListAsync();

        if (purchases.Count == 0)
        {
            logger.LogInformation("No purchase history found for user {UserId}. Returning random trending items.", userId);
            return await dbContext.FashionProducts.OrderBy(r => EF.Functions.Random()).Take(5).ToListAsync();
        }

        // 2. Use AI to analyze their "Style DNA"
        var historySummary = string.Join("\n", purchases.Select(p => $"- {p.Product?.ProductName} ({p.Product?.Category}): {p.Product?.Description}"));
        
        var analysis = await AnalyzeStyleDnaAsync(historySummary);
        logger.LogInformation("Style DNA Analysis Complete: {Summary}", analysis.StyleSummary);

        // 3. Search for items that match the "Recommended Qualities"
        var inventory = await dbContext.FashionProducts
            .Where(p => !purchases.Select(pur => pur.ProductId).Contains(p.Id)) // Don't recommend what they already bought
            .ToListAsync();

        var recommendations = inventory
            .Select(p => {
                double score = 0;
                var pText = $"{p.ProductName} {p.Description} {p.Color} {p.Category}".ToLower();
                
                foreach (var quality in analysis.RecommendedQualities)
                {
                    if (pText.Contains(quality.ToLower())) score += 5.0;
                }
                
                // Bonus for items that are in the same general aesthetic but different category
                return new { Product = p, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => Guid.NewGuid())
            .Select(x => x.Product)
            .Take(8)
            .ToList();

        // Fallback if AI selection is too narrow
        if (recommendations.Count < 3)
        {
            var fallback = await dbContext.FashionProducts
                .Where(p => !purchases.Select(pur => pur.ProductId).Contains(p.Id))
                .OrderBy(r => EF.Functions.Random())
                .Take(5)
                .ToListAsync();
            recommendations.AddRange(fallback.Where(f => !recommendations.Any(r => r.Id == f.Id)));
        }

        return recommendations.Take(8).ToList();
    }

    private async Task<RecommendationAnalysis> AnalyzeStyleDnaAsync(string history)
    {
        try
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            
            var prompt = $$"""
                          You are a luxury fashion advisor. Analyze this customer's recent purchase history and describe their "Style DNA".
                          Then, provide 10 specific fashion keywords/qualities for items they would love next.
                          
                          PURCHASE HISTORY:
                          {{history}}
                          
                          Return ONLY a JSON object:
                          {
                            "StyleSummary": "A 1-sentence description of their aesthetic (e.g., 'Minimalist Monochrome', 'Eclectic Bohemian')",
                            "RecommendedQualities": ["linen", "relaxed fit", "earth tones", "..."]
                          }
                          """;

            var result = await chatService.GetChatMessageContentAsync(prompt);
            var json = result?.Content;

            if (string.IsNullOrWhiteSpace(json)) return new RecommendationAnalysis();
            if (json.Contains("```")) json = json.Split("```json").Last().Split("```").First().Trim();

            return JsonSerializer.Deserialize<RecommendationAnalysis>(json) ?? new RecommendationAnalysis();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing style DNA");
            return new RecommendationAnalysis();
        }
    }
}
