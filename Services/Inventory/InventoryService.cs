using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;
using ThryftAiServer.Services.Aws;

namespace ThryftAiServer.Services.Inventory;

public class InventoryService(
    S3Service s3Service,
    ProductEnrichmentService enrichmentService,
    AppDbContext dbContext
    )
{
    public async Task<FashionProduct> UploadAndAnalyzeProductAsync(IFormFile file)
    {
        Console.WriteLine($"Processing product upload: {file.FileName}");

        // upload to s3
        using var uploadStream = file.OpenReadStream();
        var imageUrl = await s3Service.UploadFileAsync(uploadStream, file.FileName, file.ContentType);
        
        Console.WriteLine($"Image uploaded to: {imageUrl}");

        // analyze with ai
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        var product = await enrichmentService.AnalyzeImageAsync(imageBytes, imageUrl);
        
        Console.WriteLine($"AI Analysis successful for {product.ProductName}");

        // save to database
        dbContext.FashionProducts.Add(product);
        await dbContext.SaveChangesAsync();

        return product;
    }
}
