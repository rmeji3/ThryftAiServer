using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;
using ThryftAiServer.Services.Aws;

namespace ThryftAiServer.Services.Inventory;

public class InventoryService(
    S3Service s3Service,
    ProductEnrichmentService enrichmentService,
    AppDbContext dbContext,
    ILogger<InventoryService> logger)
{
    public async Task<FashionProduct> UploadAndAnalyzeProductAsync(IFormFile file)
    {
        logger.LogInformation("Processing product upload: {FileName}", file.FileName);

        // 1. Upload to S3
        using var uploadStream = file.OpenReadStream();
        var imageUrl = await s3Service.UploadFileAsync(uploadStream, file.FileName, file.ContentType);
        
        logger.LogInformation("Image uploaded to: {ImageUrl}", imageUrl);

        // 2. Analyze with AI
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        var product = await enrichmentService.AnalyzeImageAsync(imageBytes, imageUrl);
        
        logger.LogInformation("AI Analysis successful for {ProductName}", product.ProductName);

        // 3. Save to Database
        dbContext.FashionProducts.Add(product);
        await dbContext.SaveChangesAsync();

        return product;
    }
}
