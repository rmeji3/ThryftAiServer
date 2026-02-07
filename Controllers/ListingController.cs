using Microsoft.AspNetCore.Mvc;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Listing;

namespace ThryftAiServer.Controllers;

public class CreateListingDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? MasterCategory { get; set; }
    public string? Gender { get; set; }
    public string? Color { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ListingController(
    ListingAutofillService autofillService,
    AppDbContext dbContext,
    ILogger<ListingController> logger) : ControllerBase
{
    [HttpPost("autofill")]
    public async Task<ActionResult<FashionProduct>> GetAutofillInfo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            var result = await autofillService.GetAutofillDataAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating autofill data");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("create")]
    public async Task<ActionResult<FashionProduct>> CreateListing([FromBody] CreateListingDto dto)
    {
        try
        {
            var product = new FashionProduct
            {
                ProductName = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Category = dto.Category,
                MasterCategory = dto.MasterCategory,
                Gender = dto.Gender,
                Color = dto.Color,
                Metadata = "user_listing"
            };

            dbContext.FashionProducts.Add(product);
            await dbContext.SaveChangesAsync();

            return Ok(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating listing");
            return StatusCode(500, "An error occurred while saving your listing.");
        }
    }
}
