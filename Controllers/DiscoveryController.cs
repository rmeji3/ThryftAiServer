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
    PersonalizedRecommendationService personalizationService
    ) : ControllerBase
{
    [HttpGet("personalized")]// use global-user so we dont need to create authentication
    public async Task<ActionResult<IEnumerable<FashionProduct>>> GetPersonalizedDocs([FromQuery] string userId = "global-user")
    {
        var recommendations = await personalizationService.GetPersonalizedRecommendationsAsync(userId);
        return Ok(recommendations);
    }
    [HttpGet("random")]
    public async Task<ActionResult<IEnumerable<FashionProduct>>> GetRandomItems()
    {
        // Get 10 random items from the inventory
        var items = await dbContext.FashionProducts
            .OrderBy(r => EF.Functions.Random())
            .Take(30)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("visual-search")]
    public async Task<ActionResult<IEnumerable<FashionProduct>>> VisualSearch(IFormFile file)
    {
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
            Console.WriteLine($"Error during visual search: {ex.Message}");
            return StatusCode(500, "An error occurred during visual search");
        }
    }
}
