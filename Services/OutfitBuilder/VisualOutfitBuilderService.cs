using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;

namespace ThryftAiServer.Services.OutfitBuilder;

public class VisualOutfitBuilderService(
    Kernel kernel,
    OutfitBuilderService outfitBuilderService
    )
{
    public class UserVisualItem
    {
        public byte[] ImageBytes { get; set; } = null!;
        public string Category { get; set; } = string.Empty; //  Topwear, Footwear, etc
    }

    public async Task<List<FashionProduct>> CompleteOutfitFromImagesAsync(List<UserVisualItem> userItems, string? gender = null)
    {
        // 1. Define the core categories and identify what's missing programmatically
        var coreCategories = new List<string> { "Topwear", "Bottomwear", "Footwear", "Accessories" };
        
        // Helper to map user input to core categories for gap detection
        string MapToCore(string cat) => cat.ToLower() switch
        {
            var s when s.Contains("top") || s.Contains("shirt") || s.Contains("jacket") => "Topwear",
            var s when s.Contains("bottom") || s.Contains("pant") || s.Contains("jean") || s.Contains("skirt") => "Bottomwear",
            var s when s.Contains("foot") || s.Contains("shoe") || s.Contains("sock") => "Footwear",
            var s when s.Contains("access") || s.Contains("watch") || s.Contains("belt") || s.Contains("bag") || s.Contains("wallet") => "Accessories",
            _ => "Topwear"
        };

        var presentCategories = userItems.Select(i => MapToCore(i.Category)).Distinct().ToList();
        var missingCategories = coreCategories.Where(c => !presentCategories.Contains(c)).ToList();

        // 2. Analyze all uploaded images for the stylistic "vibe"
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        
        var message = new ChatMessageContent { Role = AuthorRole.User };
        message.Items.Add(new TextContent("I am wearing the items in these images. " +
                            string.Join(" ", userItems.Select(i => $"One is a {i.Category}.")) +
                            " Analyze their collective style and aesthetic vibe. " +
                            $"Based on this, generate a prompt for a personal stylist to find matching items in these MISSING categories: {string.Join(", ", missingCategories)}. " +
                            "Return ONLY a JSON object with: 'collective_vibe' and 'styling_prompt'."));

        foreach (var item in userItems)
        {
            message.Items.Add(new ImageContent(new ReadOnlyMemory<byte>(item.ImageBytes), "image/jpeg"));
        }

        chatHistory.Add(message);

        var response = await chatService.GetChatMessageContentAsync(chatHistory);
        var json = response.Content;

        if (string.IsNullOrEmpty(json)) throw new Exception("AI failed to analyze your items.");
        if (json.Contains("```")) json = json.Split("```json").Last().Split("```").First().Trim();

        using var doc = JsonDocument.Parse(json);
        var stylingPrompt = doc.RootElement.GetProperty("styling_prompt").GetString() ?? "";

        // 3. Get recommendations based on the stylistic vibe
        var recommendations = await outfitBuilderService.GetOutfitRecommendationsAsync(stylingPrompt, gender);

        // 4. Deterministic Filter: Strictly ONLY include items from categories that aren't in the user's list
        return recommendations
            .Where(p => missingCategories.Any(missing => 
                string.Equals(p.MasterCategory, missing, StringComparison.OrdinalIgnoreCase) ||
                (missing == "Footwear" && p.MasterCategory == "Shoes"))) // Normalize Footwear/Shoes
            .ToList();
    }
}
