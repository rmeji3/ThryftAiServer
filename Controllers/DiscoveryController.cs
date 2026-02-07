using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Discovery;
using ThryftAiServer.Services.Personalization;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController(
    AppDbContext dbContext,
    VisualSearchService visualSearchService,
    PersonalizedRecommendationService personalizationService,
    ILogger<DiscoveryController> logger) : ControllerBase
{
    [HttpGet("personalized")]
    public async Task<ActionResult<IEnumerable<FashionProduct>>> GetPersonalizedDocs([FromQuery] string userId = "global-user")
    {
        var recommendations = await personalizationService.GetPersonalizedRecommendationsAsync(userId);
        return Ok(recommendations);
    }
    [HttpGet("random")]
    public async Task<ActionResult<IEnumerable<FashionProduct>>> GetRandomEnriched()
    {
        // Get 10 random items from the inventory
        var items = await dbContext.FashionProducts
            .OrderBy(r => EF.Functions.Random())
            .Take(10)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("visual-search")]
    public async Task<ActionResult<IEnumerable<FashionProduct>>> VisualSearch(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            var results = await visualSearchService.SearchByImageAsync(imageBytes);
            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during visual search");
            return StatusCode(500, "An error occurred during visual search processing.");
        }
    }
}
