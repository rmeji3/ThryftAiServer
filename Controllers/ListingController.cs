using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("{id}")]
    public async Task<ActionResult<FashionProduct>> GetListingById(int id)
    {
        try
        {
            var product = await dbContext.FashionProducts.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting listing: {ex.Message}");
            return StatusCode(500, "An error occurred while getting your listing.");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<FashionProduct>>> searchListings([FromQuery] string search)
    {
        try
        {
            search = search.ToLower();
            var query = dbContext.FashionProducts.AsQueryable();
            query = query.Where(p => p.ProductName.ToLower().Contains(search));
            var products = await query.ToListAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching listings: {ex.Message}");
            return StatusCode(500, "An error occurred while searching your listings.");
        }
    }
}
