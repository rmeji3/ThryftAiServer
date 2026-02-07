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
        var categoriesUsed = userItems.Select(i => i.Category).ToList();

        var allCategories = new List<string> { "Topwear", "Bottomwear", "Footwear" };
        var missingCategories = allCategories.Where(cat => !categoriesUsed.Any(used => used.Contains(cat, StringComparison.OrdinalIgnoreCase))).ToList();

        // analyze all uploaded images to extract a collective "vibe"
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        
        var message = new ChatMessageContent { Role = AuthorRole.User };
        message.Items.Add(new TextContent("I am wearing the items in these images. " +
                            string.Join(" ", userItems.Select(i => $"One is a {i.Category}.")) +
                            $" Analyze their collective style, color harmony, and aesthetic vibe. I am MISSING: {string.Join(", ", missingCategories)}. " +
                            "Then, based on this analysis, generate a detailed 1-sentence prompt for a personal stylist to find the MISSING pieces to complete a stunning, cohesive look. " +
                            "IMPORTANT: Your prompt must ensure we pick AT LEAST one item from EVERY missing category to finish the outfit. " +
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
        var collectiveVibe = doc.RootElement.GetProperty("collective_vibe").GetString() ?? "";
        var stylingPrompt = doc.RootElement.GetProperty("styling_prompt").GetString() ?? "";

        // get recommendations for the full outfit based on the prompt
        var recommendations = await outfitBuilderService.GetOutfitRecommendationsAsync(stylingPrompt, gender);

        // filter out the categories the user already provided
        return recommendations
            .Where(p => !categoriesUsed.Any(cat => 
                string.Equals(p.MasterCategory, cat, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
