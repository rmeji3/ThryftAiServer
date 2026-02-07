using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ThryftAiServer.Services.OutfitBuilder;

// Structured classes for vibe analysis
public class VibeRequirement
{
    public string Category { get; set; } = string.Empty;
    public string SearchTerms { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}

public class VibeAnalysis
{
    public string OverallTheme { get; set; } = string.Empty;
    public string StylistReasoning { get; set; } = string.Empty;
    public List<int> SelectedProductIds { get; set; } = new();
}

// This service builds outfits based on a vibe using the categorized fashion dataset.
public class OutfitBuilderService(
    Kernel kernel,
    AppDbContext dbContext,
    ILogger<OutfitBuilderService> logger)
{
    public async Task<List<FashionProduct>> GetOutfitRecommendationsAsync(string vibe, string? gender = null)
    {
        // 1. Fetch entire inventory (since it's downsampled to ~150 items)
        var query = dbContext.FashionProducts.AsQueryable();
        if (!string.IsNullOrEmpty(gender))
        {
            query = query.Where(p => p.Gender == gender || p.Gender == "Unisex");
        }
        var inventory = await query.ToListAsync();

        if (inventory.Count == 0) return new List<FashionProduct>();

        // 2. Use AI as a Personal Shopper to pick from ACTUAL inventory
        var selection = await PickBestOutfitFromInventoryAsync(vibe, inventory);
        
        // 3. Map selected IDs back to products
        var outfit = inventory
            .Where(p => selection.SelectedProductIds.Contains(p.Id))
            .ToList();

        // 4. Fallback if AI selection failed
        if (outfit.Count == 0)
        {
            logger.LogWarning("AI failed to select items. Using random fallback.");
            return inventory.OrderBy(r => Guid.NewGuid()).Take(4).ToList();
        }

        // Decorate metadata with the overall theme reasoning
        foreach (var item in outfit)
        {
            item.Metadata = selection.StylistReasoning;
        }

        return outfit;
    }

    private async Task<VibeAnalysis> PickBestOutfitFromInventoryAsync(string vibe, List<FashionProduct> inventory)
    {
        try
        {
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Create a summarized inventory string for the prompt
            var itemsList = string.Join("\n", inventory.Select(p => $"ID: {p.Id} | {p.ProductName} ({p.MasterCategory}/{p.Category}) - {p.Description}"));

            var prompt = $$"""
                          You are an elite personal stylist for high-profile clients. 
                          Your goal is to "hand-pick" a perfectly cohesive outfit from the available boutique inventory based on the user's vibe.
                          
                          User Vibe: "{{vibe}}"
                          
                          AVAILABLE INVENTORY:
                          {{itemsList}}
                          
                          TASK:
                          1. Pick 3 to 5 items that create a COMPLETE and STUNNING look.
                          2. CRITICAL: You MUST include AT LEAST one Top, one Bottom, and matching Footwear/Shoes (if available in inventory).
                          3. Ensure the styles, colors, and vibes of the picked items are perfectly harmonized.
                          
                          Return ONLY a JSON object:
                          {
                            "OverallTheme": "A catchy name for this look",
                            "StylistReasoning": "A 2-sentence explanation of why these specific pieces work together for the requested vibe",
                            "SelectedProductIds": [42, 12, 85]
                          }
                          """;

            var result = await chatCompletionService.GetChatMessageContentAsync(prompt);
            var json = result?.Content;

            if (string.IsNullOrWhiteSpace(json)) return new VibeAnalysis();
            if (json.Contains("```")) json = json.Split("```json").Last().Split("```").First().Trim();

            return JsonSerializer.Deserialize<VibeAnalysis>(json) ?? new VibeAnalysis();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in AI personal shopper selection");
            return new VibeAnalysis();
        }
    }
}