using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace RagChatBot.Services;

public class ClaudeService
{
    private const string Model = "claude-sonnet-4-6";
    private readonly AnthropicClient _client;

    public ClaudeService(string apiKey)
    {
        _client = new AnthropicClient(new APIAuthentication(apiKey));
    }

    /// <summary>
    /// Asks Claude whether the user's message is a question seeking information.
    /// Returns true if it is a question, false otherwise.
    /// </summary>
    public async Task<bool> IsQuestionAsync(string message, CancellationToken ct = default)
    {
        var parameters = new MessageParameters
        {
            Model = Model,
            MaxTokens = 10,
            SystemMessage = 
            
                    "You are a classifier. Respond with exactly one word: 'yes' if the user's message " +
                    "is a question seeking information or an explanation, or 'no' if it is a greeting, " +
                    "statement, command, or anything else that does not require a factual answer.",
            Messages =
            [
                new Message(RoleType.User, message)
            ]
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters);
        var answer = response.ContentBlock.Text.Trim().ToLowerInvariant();
        return answer.StartsWith("yes");
    }

    /// <summary>
    /// Sends the user's question to Claude together with the relevant RAG chunks as context.
    /// Returns Claude's answer.
    /// </summary>
    public async Task<string> AnswerWithContextAsync(
        string question,
        List<SearchResult> context,
        CancellationToken ct = default)
    {
        // Build the context block from the top-ranked chunks
        var contextBlock = string.Join("\n\n",
            context.Select((r, i) =>
                $"[Chunk {i + 1} | similarity: {r.Score:F3}]\n{r.Chunk.Text}"));

        var systemPrompt =
            "You are a helpful assistant. You will be given relevant excerpts from a knowledge base " +
            "followed by a user question. Answer the question using the provided context. " +
            "Be concise and accurate.";

        var userMessage =
            $"Context from knowledge base:\n\n{contextBlock}\n\n---\n\nQuestion: {question}";

        var parameters = new MessageParameters
        {
            Model = Model,
            MaxTokens = 1024,
            SystemMessage = systemPrompt,
            Messages = [new Message(RoleType.User, userMessage)]
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters);
        return response.ContentBlock.Text.Trim();
    }

    /// <summary>
    /// Sends a plain conversational message to Claude (no RAG context needed).
    /// </summary>
    public async Task<string> ChatAsync(string message, CancellationToken ct = default)
    {
        var parameters = new MessageParameters
        {
            Model = Model,
            MaxTokens = 512,
            SystemMessage = "You are a friendly and helpful assistant.",
            Messages = [new Message(RoleType.User, message)]
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters);
        return response.ContentBlock.Text.Trim();
    }
}
