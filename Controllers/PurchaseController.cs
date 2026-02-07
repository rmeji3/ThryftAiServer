using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<List<Purchase>>> RecordPurchase([FromQuery] List<int> productIds, [FromQuery] string userId = "global-user")
    {
        try
        {
            var products = await dbContext.FashionProducts
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count == 0) 
            {
                return NotFound("None of the specified products were found.");
            }

            var purchases = products.Select(product => new Purchase
            {
                ProductId = product.Id,
                UserId = userId,
                PurchaseDate = DateTime.UtcNow
            }).ToList();

            dbContext.Purchases.AddRange(purchases);
            await dbContext.SaveChangesAsync();

            foreach (var product in products)
            {
                Console.WriteLine($"Recorded purchase: User {userId} bought {product.ProductName}");
            }

            return Ok(purchases);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recording purchase: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
}
