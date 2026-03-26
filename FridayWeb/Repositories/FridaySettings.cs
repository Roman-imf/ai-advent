using FridayWeb.Entities;

namespace FridayWeb.Repositories;

// Пример сущности

public class FridaySettings
{
    public Guid Id { get; set; }
    public LlmModel Model { get; set; }
    public decimal Temperature { get; set; }
    public int MaxTokens { get; set; }
}