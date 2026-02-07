using Microsoft.AspNetCore.Mvc;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Inventory;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(
    InventoryService inventoryService
    ) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<FashionProduct>> UploadProduct(IFormFile file)
    {
        try
        {
            var product = await inventoryService.UploadAndAnalyzeProductAsync(file);
            return Ok(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading and analyzing product: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
