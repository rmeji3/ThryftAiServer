using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using ThryftAiServer.Models;

namespace ThryftAiServer.Services.Ai;

public class ProductEnrichmentService(Kernel kernel)
{
    public async Task<FashionProduct> AnalyzeImageAsync(byte[] imageBytes, string imageUrl)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage([
            new TextContent("Analyze this clothing image for a fashion 'vibe' search engine. Describe it with rich fashion adjectives. " +
                            "Identify the MasterCategory (Apparel, Footwear, Accessories), Category (Topwear, Bottomwear, etc.), Gender, and Color. " +
                            "Return a JSON object with: 'title' (marketing name), 'vibe_description' (2-3 sentences), 'masterCategory', 'category', 'gender', 'color'."),
            new ImageContent(new ReadOnlyMemory<byte>(imageBytes), "image/jpeg")
        ]);

        var response = await chatService.GetChatMessageContentAsync(chatHistory);
        var json = response.Content;

        if (string.IsNullOrEmpty(json)) throw new Exception("AI failed to analyze image.");

        if (json.Contains("```")) 
        {
            json = json.Split("```json").Last().Split("```").First().Trim();
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new FashionProduct
        {
            ProductName = root.GetProperty("title").GetString(),
            Description = root.GetProperty("vibe_description").GetString(),
            MasterCategory = root.GetProperty("masterCategory").GetString(),
            Category = root.GetProperty("category").GetString(),
            Gender = root.GetProperty("gender").GetString(),
            Color = root.GetProperty("color").GetString(),
            ImageUrl = imageUrl,
            Metadata = "user_uploaded"
        };
    }
}
