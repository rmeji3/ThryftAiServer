namespace ThryftAiServer.Dtos;

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