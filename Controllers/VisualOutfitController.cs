using Microsoft.AspNetCore.Mvc;
using ThryftAiServer.Models;
using ThryftAiServer.Services.OutfitBuilder;

namespace ThryftAiServer.Controllers;

public class MultiVisualOutfitRequest
{
    public List<IFormFile> Files { get; set; } = new();
    public List<string> Categories { get; set; } = new(); // Map to Files by index
    public string? Gender { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class VisualOutfitController(
    VisualOutfitBuilderService visualOutfitService,
    ILogger<VisualOutfitController> logger) : ControllerBase
{
    [HttpPost("complete-look")]
    public async Task<ActionResult<List<FashionProduct>>> CompleteLook([FromForm] MultiVisualOutfitRequest request)
    {
        if (request.Files == null || request.Files.Count == 0)
            return BadRequest("No images were uploaded.");

        try
        {
            var userItems = new List<VisualOutfitBuilderService.UserVisualItem>();

            for (int i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                // Get corresponding category or default to "Apparel"
                var category = request.Categories.Count > i ? request.Categories[i] : "Apparel";

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                userItems.Add(new VisualOutfitBuilderService.UserVisualItem
                {
                    ImageBytes = ms.ToArray(),
                    Category = category
                });
            }

            var recommendations = await visualOutfitService.CompleteOutfitFromImagesAsync(userItems, request.Gender);
            
            return recommendations.Any() ? Ok(recommendations) : NotFound("Could not find suitable items to complete this look.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing outfit from visual input");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
