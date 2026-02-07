namespace ThryftAiServer.Controllers;

using Microsoft.AspNetCore.Mvc;
using ThryftAiServer.Services.OutfitBuilder;
using Microsoft.AspNetCore.Authorization;
using ThryftAiServer.Models;


[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class OutfitBuilderController : ControllerBase
{
    OutfitBuilderService outfitBuilderService;

    public OutfitBuilderController(OutfitBuilderService outfitBuilderService)
    {
        this.outfitBuilderService = outfitBuilderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FashionProduct>>> GetOutfitRecommendations([FromQuery] string vibe, [FromQuery] string? gender)
    {
        var recommendations = await outfitBuilderService.GetOutfitRecommendationsAsync(vibe, gender);
        if (recommendations.Any())
        {
            return Ok(recommendations);
        }
        return NotFound("No matching fashion items found for this vibe.");
    }
}