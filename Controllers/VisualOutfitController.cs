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
    VisualOutfitBuilderService visualOutfitService
    ) : ControllerBase
{
    [HttpPost("complete-look")]// this endpoint is used to complete a look from uploading images of your current outfit
    public async Task<ActionResult<List<FashionProduct>>> CompleteLook([FromForm] MultiVisualOutfitRequest request)
    {
        try
        {
            var userItems = new List<VisualOutfitBuilderService.UserVisualItem>();

            for (int i = 0; i < request.Files.Count; i++)// loop through the files and add them to the list of items they currently have on
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
            
            if (recommendations.Any())
            {
                return Ok(recommendations);
            }
            return NotFound("Could not find suitable items to complete this look :(");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error completing outfit from visual input: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
