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
    public async Task<List<FashionProduct>> GetOutfitRecommendationsAsync(string vibe, string? gender = null)
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
        var selection = await PickBestOutfitFromInventoryAsync(vibe, inventory);
        
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

        return outfit;
    }

    private async Task<VibeAnalysis> PickBestOutfitFromInventoryAsync(string vibe, List<FashionProduct> inventory)
    {
        try
        {
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // create a summarized inventory string for the prompt
            var itemsList = string.Join("\n", inventory.Select(p => $"ID: {p.Id} | {p.ProductName} ({p.MasterCategory}/{p.Category}) - {p.Description}"));

            var prompt = $$"""
                          You are a professional personal stylist for clients with a strong sense of style. 
                          Your goal is to "hand-pick" a perfectly paired outfit from the available inventory based on the user's vibe.
                          
                          User Vibe: "{{vibe}}"
                          
                          available inventory:
                          {{itemsList}}
                          
                          task:
                          1. Pick 3 to 5 items that create an outfit that matches the user's vibe. make sure the outfit matches!!!
                          2. must: include AT LEAST one Top, one Bottom, and matching Footwear/Shoes (if available in inventory).
                          3. make sure the styles, colors, and vibes of the picked items are perfectly matched.
                          
                          Return only a JSON object:
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
            return new VibeAnalysis();
        }
    }
}