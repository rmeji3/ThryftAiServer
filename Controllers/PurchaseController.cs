using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Data.App;
using ThryftAiServer.Models;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseController(AppDbContext dbContext, ILogger<PurchaseController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Purchase>> RecordPurchase([FromQuery] int productId, [FromQuery] string userId = "global-user")
    {
        try
        {
            var product = await dbContext.FashionProducts.FindAsync(productId);
            if (product == null) return NotFound("Product not found");

            var purchase = new Purchase
            {
                ProductId = productId,
                UserId = userId,
                PurchaseDate = DateTime.UtcNow
            };

            dbContext.Purchases.Add(purchase);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Recorded purchase: User {UserId} bought {ProductName}", userId, product.ProductName);
            return Ok(purchase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording purchase");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchaseHistory([FromQuery] string userId = "global-user")
    {
        var history = await dbContext.Purchases
            .Include(p => p.Product)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();

        return Ok(history);
    }
}
