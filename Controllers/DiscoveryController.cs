using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("random")]
    public async Task<ActionResult<IEnumerable<FashionProduct>>> GetRandomEnriched()
    {
        // Get 10 random items that have been enriched by the vision AI
        var items = await dbContext.FashionProducts
            .Where(p => p.Metadata == "enriched_by_vision")
            .OrderBy(r => EF.Functions.Random())
            .Take(10)
            .ToListAsync();

        if (!items.Any())
        {
            // Fallback: If enrichment hasn't finished yet, just give 10 random items
            items = await dbContext.FashionProducts
                .OrderBy(r => EF.Functions.Random())
                .Take(10)
                .ToListAsync();
        }

        return Ok(items);
    }
}
