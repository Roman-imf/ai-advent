namespace FridayWeb.Entities;

public class FridaySettings
{
    public LlmModel Model { get; set; }
    public decimal Temperature { get; set; }
    public int MaxTokens { get; set; }
}

public enum LlmModel
{
    Haiku = 0,
    Sonnet = 1,
    Opus = 2,
}