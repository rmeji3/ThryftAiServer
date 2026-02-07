using Microsoft.AspNetCore.Mvc;
using ThryftAiServer.Models;
using ThryftAiServer.Services.Inventory;

namespace ThryftAiServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(
    InventoryService inventoryService,
    ILogger<InventoryController> logger) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<FashionProduct>> UploadProduct(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            var product = await inventoryService.UploadAndAnalyzeProductAsync(file);
            return Ok(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading and analyzing product");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
