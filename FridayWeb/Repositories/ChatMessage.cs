using FridayWeb.Entities;

namespace FridayWeb.Repositories;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Input { get; set; }
    public string Output { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public LlmModel Model { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal ElapsedSeconds { get; set; }
    
    public string Summary { get; set; }
}