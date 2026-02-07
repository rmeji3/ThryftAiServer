using ThryftAiServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Formats.Asn1;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

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

    public static async Task EnrichWithVisionAsync(AppDbContext context, Kernel kernel, string imagePath, int limit = 50)
    {
        var productsToEnrich = await context.FashionProducts
            .Where(p => p.Description!.Contains("DeepFashion")) // Only enrich generic items
            .Take(limit)
            .ToListAsync();

        if (!productsToEnrich.Any()) return;

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        Console.WriteLine($"Enriching {productsToEnrich.Count} items with Vision AI for better search...");
        int count = 0;

        foreach (var product in productsToEnrich)
        {
            bool success = false;
            int retries = 0;

            while (!success && retries < 3)
            {
                try
                {
                    // Extract original filename from ExternalId (e.g. 000001_item1 -> 000001.jpg)
                    var baseFileName = product.ExternalId!.Split('_')[0];
                    var fullImagePath = Path.Combine(imagePath, $"{baseFileName}.jpg");

                    if (!File.Exists(fullImagePath))
                    {
                        success = true; // Mark skip as "success" to move on
                        continue;
                    }

                    var imageBytes = await File.ReadAllBytesAsync(fullImagePath);
                    var chatHistory = new ChatHistory();
                    chatHistory.AddUserMessage([
                        new TextContent($"Analyze this clothing image. Describe it like a high-end fashion curator. Focus on style (e.g., 'Retro-inspired', 'Streetwear-ready'), material, and unique details. Return a JSON object with: 'name' (a punchy marketing name) and 'description' (a rich 1-2 sentence description). Only return the JSON."),
                        new ImageContent(new ReadOnlyMemory<byte>(imageBytes), "image/jpeg")
                    ]);

                    var response = await chatService.GetChatMessageContentAsync(chatHistory);
                    var json = response.Content;

                    if (string.IsNullOrEmpty(json)) throw new Exception("AI returned empty response");

                    // Simple JSON cleaning
                    if (json.Contains("```")) 
                    {
                        var parts = json.Split("```json");
                        if (parts.Length > 1) json = parts[1].Split("```")[0].Trim();
                        else json = json.Split("```")[1].Split("```")[0].Trim();
                    }

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    product.ProductName = root.GetProperty("name").GetString();
                    product.Description = root.GetProperty("description").GetString();
                    product.Metadata = "enriched_by_vision";

                    Console.WriteLine($"[{++count}/{productsToEnrich.Count}] Enriched: {product.ProductName}");
                    success = true;
                    
                    // Periodic save
                    if (count % 10 == 0) await context.SaveChangesAsync();

                    // Standard delay to stay under TPM limits
                    await Task.Delay(1000); 
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("rate_limit") || ex.Message.Contains("429"))
                    {
                        retries++;
                        Console.WriteLine($"Rate limit hit for {product.ExternalId}. Retry {retries}/3. Waiting 10s...");
                        await Task.Delay(10000);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to enrich {product.ExternalId}: {ex.Message}");
                        success = true; // Move on for non-rate-limit errors
                    }
                }
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("Vision enrichment complete.");
    }

    public static async Task MigrateFromSqliteAsync(AppDbContext postgresContext, string sqliteConnectionStr)
    {
        Console.WriteLine("Starting migration from local SQLite to AWS RDS...");

        // Create a temporary options builder for the local SQLite DB
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(sqliteConnectionStr);

        using (var localContext = new AppDbContext(optionsBuilder.Options))
        {
            var enrichedItems = await localContext.FashionProducts
                .Where(p => p.Metadata == "enriched_by_vision")
                .AsNoTracking() // Important so EF doesn't get confused by tracking across contexts
                .ToListAsync();

            if (!enrichedItems.Any())
            {
                Console.WriteLine("No enriched items found in local SQLite to migrate.");
                return;
            }

            Console.WriteLine($"Found {enrichedItems.Count} enriched items. Pushing to RDS...");

            // Reset IDs for Postgres (let it generate new ones)
            foreach (var item in enrichedItems)
            {
                item.Id = 0; 
            }

            // Check if they already exist in Postgres to prevent duplicates
            var existingExternalIds = await postgresContext.FashionProducts
                .Select(p => p.ExternalId)
                .ToListAsync();

            var newItems = enrichedItems
                .Where(item => !existingExternalIds.Contains(item.ExternalId))
                .ToList();

            if (newItems.Any())
            {
                await postgresContext.FashionProducts.AddRangeAsync(newItems);
                await postgresContext.SaveChangesAsync();
                Console.WriteLine($"Successfully migrated {newItems.Count} items to AWS RDS.");
            }
            else
            {
                Console.WriteLine("All items already exist in RDS. Nothing to migrate.");
            }
        }
    }

    public static async Task UpdateUrlsToS3Async(AppDbContext context, string s3BaseUrl)
    {
        Console.WriteLine($"Updating all image URLs to point to S3: {s3BaseUrl}");
        
        var products = await context.FashionProducts.ToListAsync();
        foreach (var p in products)
        {
            if (p.ImageUrl != null && p.ImageUrl.StartsWith("/images/"))
            {
                var fileName = Path.GetFileName(p.ImageUrl);
                p.ImageUrl = $"{s3BaseUrl.TrimEnd('/')}/images/{fileName}";
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("Database URLs updated to S3 successfully.");
    }

    public static async Task SeedFromCsvAsync(AppDbContext context, string stylesCsvPath, string imagesCsvPath)
    {
        Console.WriteLine("Clearing existing products for new dataset...");
        context.FashionProducts.RemoveRange(context.FashionProducts);
        await context.SaveChangesAsync();

        Console.WriteLine("Reading CSV files...");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var stylesReader = new StreamReader(stylesCsvPath);
        using var csvStyles = new CsvReader(stylesReader, config);
        var styleRecords = csvStyles.GetRecords<StyleCsvRecord>().ToList();

        using var imagesReader = new StreamReader(imagesCsvPath);
        using var csvImages = new CsvReader(imagesReader, config);
        var imageRecords = csvImages.GetRecords<ImageCsvRecord>().ToList();

        Console.WriteLine($"Found {styleRecords.Count} styles and {imageRecords.Count} images.");

        // Join them
        var imageMap = imageRecords.ToDictionary(r => Path.GetFileNameWithoutExtension(r.filename), r => r.link);

        var products = new List<FashionProduct>();
        int count = 0;
        foreach (var style in styleRecords)
        {
            if (imageMap.TryGetValue(style.id, out var imageUrl))
            {
                products.Add(new FashionProduct
                {
                    ExternalId = style.id,
                    ProductName = style.productDisplayName,
                    Gender = style.gender,
                    MasterCategory = style.masterCategory,
                    Category = style.subCategory,
                    FashionCategory = style.articleType,
                    Color = style.baseColour,
                    Season = style.season,
                    Year = int.TryParse(style.year, out var y) ? y : null,
                    Usage = style.usage,
                    ImageUrl = imageUrl,
                    Description = $"{style.gender} {style.articleType} in {style.baseColour} from {style.masterCategory} section."
                });
            }

            if (products.Count >= 2000)
            {
                await context.FashionProducts.AddRangeAsync(products);
                await context.SaveChangesAsync();
                count += products.Count;
                Console.WriteLine($"Seeded {count} records...");
                products.Clear();
            }
        }

        if (products.Any())
        {
            await context.FashionProducts.AddRangeAsync(products);
            await context.SaveChangesAsync();
            count += products.Count;
        }

        Console.WriteLine($"CSV Seeding complete. Total: {count}");
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

public class StyleCsvRecord
{
    public string id { get; set; }
    public string gender { get; set; }
    public string masterCategory { get; set; }
    public string subCategory { get; set; }
    public string articleType { get; set; }
    public string baseColour { get; set; }
    public string season { get; set; }
    public string year { get; set; }
    public string usage { get; set; }
    public string productDisplayName { get; set; }
}

public class ImageCsvRecord
{
    public string filename { get; set; }
    public string link { get; set; }
}
