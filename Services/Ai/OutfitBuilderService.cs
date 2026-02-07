using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ThryftAiServer.Services.OutfitBuilder;

// This service builds outfits based on a vibe using the categorized fashion dataset.
public class OutfitBuilderService(
    Kernel kernel,
    AppDbContext dbContext,
    ILogger<OutfitBuilderService> logger)
{
    public async Task<List<FashionProduct>> GetOutfitRecommendationsAsync(string vibe)
    {
        // 1. Use AI to extract relevant fashion keywords from the vibe
        var searchTerms = await AnalyzeVibeAsync(vibe);
        logger.LogInformation("Vibe '{Vibe}' analyzed into search terms: {Terms}", vibe, string.Join(", ", searchTerms));

        if (searchTerms.Count == 0)
        {
            searchTerms = [vibe];
        }

        // 2. Search FashionProducts for matching items
        var matches = await SearchProductsAsync(searchTerms);
        
        // 3. Fallback: If no matches found, just return a diverse set of fashion items
        if (!matches.Any())
        {
            logger.LogInformation("No specific matches found for vibe '{Vibe}'. Returning general fashion items as fallback.", vibe);
            matches = await dbContext.FashionProducts
                .Where(p => p.FashionCategory != "Home" && p.FashionCategory != "Other")
                .OrderBy(r => EF.Functions.Random()) // Random variety for SQLite
                .Take(6)
                .ToListAsync();
        }

        return matches;
    }

    private async Task<List<string>> AnalyzeVibeAsync(string vibe)
    {
        try
        {
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Give the AI our specific categories so it knows what to search for
            var categories = "long sleeve dress, short sleeve dress, trousers, shorts, skirt, sling, vest, long sleeve top, short sleeve top, long sleeve outwear, short sleeve outwear";

            var prompt = $"""
                          You are a fashion stylist. Translate a "vibe" or "feeling" into actual database categories.
                          User Vibe: "{vibe}"
                          
                          Available categories: {categories}
                          
                          Pick 3-5 keywords that characterize this vibe. At least 2 MUST be from the available categories list. 
                          Output a simple comma-separated list. No other text.
                          """;

            var result = await chatCompletionService.GetChatMessageContentAsync(prompt);
            var text = result.Content;

            if (string.IsNullOrWhiteSpace(text)) return [vibe];

            return text.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing vibe with AI");
            return [vibe];
        }
    }

    private async Task<List<FashionProduct>> SearchProductsAsync(List<string> searchTerms)
    {
        // Define which categories we want to include in an "outfit"
        var validFashionCategories = new[] { "Tops", "Bottoms", "One-Piece", "Outerwear", "Shoes", "Accessories" };

        var query = dbContext.FashionProducts
            .Where(p => validFashionCategories.Contains(p.FashionCategory));

        var fashionItems = await query.ToListAsync();

        var rankedMatches = fashionItems
            .Where(p => searchTerms.Any(term => 
                (p.ProductName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            ))
            .GroupBy(p => p.FashionCategory!)
            .SelectMany(g => g.Take(2)) // Take top 2 from each group for variety
            .ToList();

        return rankedMatches;
    }
}