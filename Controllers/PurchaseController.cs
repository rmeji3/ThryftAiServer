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
    public async Task<ActionResult<Purchase>> RecordPurchase([FromQuery] int productId, [FromQuery] string userId = "global-user")
    {
        try
        {
            var product = await dbContext.FashionProducts.FindAsync(productId);
            if (product == null) 
                return NotFound("Product not found");

            var purchase = new Purchase
            {
                ProductId = productId,
                UserId = userId,
                PurchaseDate = DateTime.UtcNow
            };

            dbContext.Purchases.Add(purchase);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"Recorded purchase: User {userId} bought {product.ProductName}");
            return Ok(purchase);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recording purchase: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
}
