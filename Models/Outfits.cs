namespace ThryftAiServer.Models;

public class Outfit
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<FashionProduct> Items { get; set; } = new();
}