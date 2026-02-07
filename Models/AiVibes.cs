namespace ThryftAiServer.Models;

public class VibeRequirement
{
    public string Category { get; set; } = string.Empty;
    public string SearchTerms { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}

public class VibeAnalysis
{
    public string OverallTheme { get; set; } = string.Empty;
    public string StylistReasoning { get; set; } = string.Empty;
    public List<int> SelectedProductIds { get; set; } = new();
}