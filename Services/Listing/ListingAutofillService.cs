using ThryftAiServer.Models;
using ThryftAiServer.Services.Ai;
using ThryftAiServer.Services.Aws;

namespace ThryftAiServer.Services.Listing;

public class ListingAutofillService(
    S3Service s3Service,
    ProductEnrichmentService enrichmentService,
    ILogger<ListingAutofillService> logger)
{// this service is to autofill the description of a product using the ai
    public async Task<FashionProduct> GetAutofillDataAsync(IFormFile file)
    {
        logger.LogInformation("Processing listing autofill request for: {FileName}", file.FileName);

        // upload to s3 so we have a permanent URL for the user's listing
        using var stream = file.OpenReadStream();
        var imageUrl = await s3Service.UploadFileAsync(stream, file.FileName, file.ContentType);

        // get bytes for ai analysis
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        // analyze with ai using our existing enrichment engine
        var analysis = await enrichmentService.AnalyzeImageAsync(imageBytes, imageUrl);

        logger.LogInformation("Autofill data generated for {ProductName}", analysis.ProductName);

        return analysis;
    }
}
