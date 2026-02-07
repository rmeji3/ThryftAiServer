using System.ComponentModel.DataAnnotations;

namespace ThryftAiServer.Models;

public class Purchase
{
    [Key]
    public int Id { get; set; }
    public string UserId { get; set; } = "global-user"; // Default to global user as requested
    public int ProductId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public FashionProduct? Product { get; set; }
}
