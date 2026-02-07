using System.ComponentModel.DataAnnotations;

namespace ThryftAiServer.Models;

public class FashionProduct
{
    [Key]
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string? ProductName { get; set; }
    public string? Gender { get; set; }
    public string? MasterCategory { get; set; }
    public string? Category { get; set; } // Map from subCategory
    public string? FashionCategory { get; set; } // Map from articleType
    public string? Color { get; set; } // Map from baseColour
    public string? Season { get; set; }
    public int? Year { get; set; }
    public string? Usage { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Metadata { get; set; } // For any extra AI-extracted info
    public string? Brand { get; set; }
}
