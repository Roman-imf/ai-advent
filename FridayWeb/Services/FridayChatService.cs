using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using FridayWeb.Entities;
using FridayWeb.Repositories;
using FridayWeb.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FridaySettings = FridayWeb.Entities.FridaySettings;

namespace FridayWeb.Services;

public class FridayChatService : IFridayChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AnthropicClient _client;

    public FridayChatService(ApplicationDbContext dbContext, IOptions<AnthropicConfiguration> configuration)
    {
        _dbContext = dbContext;
        _client = new() { ApiKey = configuration.Value.ApiKey };
    }

    private static readonly Dictionary<LlmModel, string> ModelIdMapping = new()
    {
        { LlmModel.Haiku, "claude-haiku-4-5" },
        { LlmModel.Sonnet, "claude-sonnet-4-5" },
        { LlmModel.Opus, "claude-opus-4-6" },
    };

    public async Task<FridayResponse> SendMessageAsync(string message)
    {
        var settings = await _dbContext.Settings.FirstAsync();
        var oldMessages = await _dbContext.Messages.ToListAsync();
        var messages = new List<MessageParam>();
        foreach (var oldMessage in oldMessages)
        {
            messages.Add(new()
            {
                Role = Role.User,
                Content = oldMessage.Input
            });
            messages.Add(new()
            {
                Role = Role.Assistant,
                Content = oldMessage.Output
            });
        }

        messages.Add(new()
        {
            Role = Role.User,
            Content = message
        });

        MessageCreateParams parameters = new()
        {
            MaxTokens = settings.MaxTokens,
            Temperature = (double)settings.Temperature,
            Messages = messages,
            Model = ModelIdMapping[settings.Model],
        };

        var sw = new Stopwatch();
        sw.Start();
        var response = await _client.Messages.Create(parameters);
        sw.Stop();
        var responseBlocks = response
            .Content
            .Select(x =>
                JsonSerializer.Deserialize<ClaudeResponse>(x.Json.GetRawText()));
        var responseMessage = responseBlocks.First().Text;

        await _dbContext.Messages.AddAsync(new ChatMessage
        {
            Input = message,
            Output = responseMessage,
            CreatedAt = DateTime.UtcNow,
            Model = settings.Model,
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens,
            ElapsedSeconds = (decimal)Math.Round(sw.Elapsed.TotalSeconds, 2, MidpointRounding.ToZero),
        });
        await _dbContext.SaveChangesAsync();

        var costs = new Dictionary<LlmModel, (decimal Input, decimal Output)>
        {
            { LlmModel.Haiku, (Input: 1 / (decimal)1000000, Output: 5 / (decimal)1000000) },
            { LlmModel.Sonnet, (Input: 3 / (decimal)1000000, Output: 15 / (decimal)1000000) },
            { LlmModel.Opus, (Input: 5 / (decimal)1000000, Output: 25 / (decimal)1000000) }
        };

        return new FridayResponse
        {
            Message = responseMessage,
            Cost = response.Usage.InputTokens * costs[settings.Model].Input +
                   response.Usage.OutputTokens * costs[settings.Model].Output,
            TokensUsed = response.Usage.InputTokens + response.Usage.OutputTokens
        };
    }

    public async Task SaveSettingsAsync(FridaySettings newSettings)
    {
        var settings = await _dbContext.Settings.FirstAsync();
        settings.Model = newSettings.Model;
        settings.MaxTokens = newSettings.MaxTokens;
        settings.Temperature = newSettings.Temperature;
        _dbContext.Settings.Update(settings);
        await _dbContext.SaveChangesAsync();
    }
}

public class ClaudeResponse
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("text")] public string Text { get; set; }
}