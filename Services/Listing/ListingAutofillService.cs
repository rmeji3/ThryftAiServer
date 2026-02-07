using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;
using ThryftAiServer.Services.Aws;

namespace ThryftAiServer.Services.Listing;

public class ListingAutofillService(
    S3Service s3Service,
    ProductEnrichmentService enrichmentService,
    ILogger<ListingAutofillService> logger)
{
    public async Task<FashionProduct> GetAutofillDataAsync(IFormFile file)
    {
        logger.LogInformation("Processing listing autofill request for: {FileName}", file.FileName);

        // 1. Upload to S3 so we have a permanent URL for the user's listing
        using var stream = file.OpenReadStream();
        var imageUrl = await s3Service.UploadFileAsync(stream, file.FileName, file.ContentType);

        // 2. Get bytes for AI analysis
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        // 3. Analyze with AI using our existing enrichment engine
        var analysis = await enrichmentService.AnalyzeImageAsync(imageBytes, imageUrl);

        logger.LogInformation("Autofill data generated for {ProductName}", analysis.ProductName);

        return analysis;
    }
}
