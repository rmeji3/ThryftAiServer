using System.ComponentModel.DataAnnotations;

namespace ThryftAiServer.Models;

public class FashionProduct
{
    [Key]
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? FashionCategory { get; set; }
    public string? Metadata { get; set; } // For any extra AI-extracted info
}
