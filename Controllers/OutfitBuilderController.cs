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
    private readonly OutfitBuilderService _outfitBuilderService;

    public OutfitBuilderController(OutfitBuilderService outfitBuilderService)
    {
        _outfitBuilderService = outfitBuilderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FashionProduct>>> GetOutfitRecommendations([FromQuery] string vibe, [FromQuery] string? gender)
    {
        var recommendations = await _outfitBuilderService.GetOutfitRecommendationsAsync(vibe, gender);
        return recommendations.Any() ? Ok(recommendations) : NotFound("No matching fashion items found for this vibe.");
    }
}