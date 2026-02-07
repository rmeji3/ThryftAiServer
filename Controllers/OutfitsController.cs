using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutfitsController(
    AppDbContext dbContext
    ) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Outfit>>> GetOutfits()
    {
        try
        {
            var outfits = await dbContext.Outfits
                .Include(o => o.Items)
                .ToListAsync();
            return Ok(outfits);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting outfits: {ex.Message}");
            return StatusCode(500, "An error occurred while getting your outfits.");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Outfit>> CreateOutfit([FromBody] Outfit outfit)
    {
        Console.WriteLine("[DEBUG V2] CreateOutfit called");
        try
        {
            // Extract IDs of items provided in the request
            var itemIds = outfit.Items.Select(i => i.Id).Distinct().ToList();

            // Fetch the actual instances from the database
            // This ensures EF Core tracks them as existing entities rather than trying to INSERT them
            var existingProducts = await dbContext.FashionProducts
                .Where(p => itemIds.Contains(p.Id))
                .ToListAsync();

            if (existingProducts.Count != itemIds.Count)
            {
                // Optional: handle cases where some items might not exist
                Console.WriteLine("Warning: Some items in the outfit request were not found in the database.");
            }

            // Assign the tracked entities to the outfit
            outfit.Items = existingProducts;

            dbContext.Outfits.Add(outfit);
            await dbContext.SaveChangesAsync();
            
            return Ok(outfit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating outfit: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, $"An error occurred while creating your outfit: {ex.Message}");
        }
    }
}
