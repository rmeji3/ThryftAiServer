using Microsoft.AspNetCore.Mvc;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Listing;
using ThryftAiServer.Dtos;

namespace ThryftAiServer.Controllers;



[ApiController]
[Route("api/[controller]")]
public class ListingController(
    ListingAutofillService autofillService,
    AppDbContext dbContext
    ) : ControllerBase
{
    // this endpoint is used to get autofill the images description
    [HttpPost("autofill")]
    public async Task<ActionResult<FashionProduct>> GetAutofillInfo(IFormFile file)
    {
        try
        {
            var result = await autofillService.GetAutofillDataAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating autofill data: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("create")]
    public async Task<ActionResult<FashionProduct>> CreateListing([FromBody] CreateListingDto dto)
    {
        try
        {
            var product = new FashionProduct // map the dto to model
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
            Console.WriteLine($"Error creating listing: {ex.Message}");
            return StatusCode(500, "An error occurred while saving your listing.");
        }
    }
}
