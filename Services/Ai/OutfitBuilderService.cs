using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ThryftAiServer.Services.OutfitBuilder;


// This service builds outfits based on a vibe using the categorized fashion dataset.
public class OutfitBuilderService(
    Kernel kernel,
    AppDbContext dbContext
    )
{
    public async Task<List<FashionProduct>> GetOutfitRecommendationsAsync(string vibe, string? gender = null, List<string>? targetCategories = null)
    {
        // first we fetch the entire inventory since it's only like 150 items
        var query = dbContext.FashionProducts.AsQueryable();
        if (!string.IsNullOrEmpty(gender))
        {
            query = query.Where(p => p.Gender == gender || p.Gender == "Unisex");
        }
        var inventory = await query.ToListAsync();

        if (inventory.Count == 0) return new List<FashionProduct>(); // if no inventory, return empty list

        // then use AI as a Personal Shopper to pick from inventory
        var selection = await PickBestOutfitFromInventoryAsync(vibe, inventory, targetCategories);
        
        // map selected IDs back to products
        var outfit = inventory
            .Where(p => selection.SelectedProductIds.Contains(p.Id))
            .ToList();

        // if we have no ai results then just give random items
        if (outfit.Count == 0)
        {
            return inventory.OrderBy(r => Guid.NewGuid()).Take(4).ToList();
        }

        // add some metadata with the overall theme reasoning, we can use this to display the reasoning in the frontend
        foreach (var item in outfit)
        {
            item.Metadata = selection.StylistReasoning;
        }
        
        // sort the list in order of top, bottom, footwear, accessories
        var categoryOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Topwear", 1 },
            { "Bottomwear", 2 },
            { "Footwear", 3 },
            { "Shoes", 3 },
            { "Accessories", 4 }
        };

        outfit = outfit.OrderBy(p => categoryOrder.TryGetValue(p.MasterCategory ?? "", out var order) ? order : 5).ToList();

        return outfit;
    }

    private async Task<VibeAnalysis> PickBestOutfitFromInventoryAsync(string vibe, List<FashionProduct> inventory, List<string>? targetCategories = null)
    {
        try
        {
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // create a summarized inventory string for the prompt
            var itemsList = string.Join("\n", inventory.Select(p => $"ID: {p.Id} | {p.ProductName} ({p.MasterCategory}/{p.Category}) - {p.Description}"));
            
            // Add a hint about category naming conventions
            itemsList += "\n(Note: 'Footwear' items are listed under the 'Shoes' MasterCategory in the inventory above)";

            var categoryGoal = targetCategories != null && targetCategories.Count > 0 
                ? $"CRITICAL: You MUST pick items strictly for these categories: {string.Join(", ", targetCategories)}."
                : "CRITICAL: You MUST include AT LEAST one Top, one Bottom, and matching Footwear/Shoes (if available in inventory).";

            var quantityGoal = targetCategories != null && targetCategories.Count > 0
                ? $"Pick 1 to 2 items for EVERY category listed above."
                : "Pick 3 to 5 items that create a COMPLETE and STUNNING look.";

            var prompt = $$"""
                          You are a professional personal stylist for clients with a strong sense of style. 
                          Your goal is to "hand-pick" the best items from the available inventory based on the user's vibe.
                          
                          User Vibe: "{{vibe}}"
                          
                          available inventory:
                          {{itemsList}}
                          
                          task:
                          1. {{quantityGoal}}
                          2. {{categoryGoal}}
                          3. make sure the styles, colors, and vibes of the picked items are perfectly matched.
                          
                          Return only a JSON object:
                          {
                            "OverallTheme": "A catchy name for this look",
                            "StylistReasoning": "A 2-sentence explanation of why these specific pieces work together",
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
            return new VibeAnalysis();
        }
    }
}