using ThryftAiServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ThryftAiServer.Data.App;

public static class DataSeeder
{
    public static async Task SeedDataAsync(AppDbContext context, string imagePath, string annosPath)
    {
        if (await context.FashionProducts.AnyAsync())
        {
            return; // Data already seeded
        }

        if (!Directory.Exists(annosPath))
        {
            Console.WriteLine($"Annotations directory not found: {annosPath}");
            return;
        }

        var jsonFiles = Directory.GetFiles(annosPath, "*.json").Take(5000).ToList();
        var records = new List<FashionProduct>();

        Console.WriteLine($"Starting bulk JSON seeding for {jsonFiles.Count} products...");

        foreach (var jsonPath in jsonFiles)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var fileName = Path.GetFileNameWithoutExtension(jsonPath);
                
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                // DeepFashion JSONs have "item1", "item2", etc.
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.StartsWith("item"))
                    {
                        var item = property.Value;
                        if (item.TryGetProperty("category_name", out var catNameElement))
                        {
                            var rawCat = catNameElement.GetString() ?? "other";
                            var fashionCat = MapDeepFashionCategory(rawCat);
                            
                            records.Add(new FashionProduct
                            {
                                ExternalId = $"{fileName}_{property.Name}",
                                ProductName = rawCat.Replace("_", " ").ToUpper(),
                                Category = rawCat,
                                FashionCategory = fashionCat,
                                Description = $"DeepFashion {rawCat} entry from {fileName}",
                                ImageUrl = $"/images/{fileName}.jpg"
                            });
                        }
                    }
                }

                if (records.Count >= 1000) // Batch save to avoid memory issues
                {
                    await context.FashionProducts.AddRangeAsync(records);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Seeded {records.Count} items so far...");
                    records.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {jsonPath}: {ex.Message}");
            }
        }

        if (records.Any())
        {
            await context.FashionProducts.AddRangeAsync(records);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("DeepFashion seeding complete.");
    }

    private static string MapDeepFashionCategory(string label)
    {
        label = label.ToLower();
        if (label.Contains("shirt") || label.Contains("top") || label.Contains("vest") || label.Contains("sling"))
            return "Tops";
        if (label.Contains("shorts") || label.Contains("trousers") || label.Contains("skirt") || label.Contains("pants"))
            return "Bottoms";
        if (label.Contains("outwear") || label.Contains("jacket") || label.Contains("coat"))
            return "Outerwear";
        if (label.Contains("dress"))
            return "One-Piece";
        
        return "Other";
    }
}
