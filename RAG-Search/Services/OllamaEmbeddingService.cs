using System.Net.Http.Json;

namespace RagChatBot.Services;

public class OllamaEmbeddingService : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaEmbeddingService(string model = "nomic-embed-text", string baseUrl = "http://localhost:11434")
    {
        _model = model;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var request = new OllamaEmbedRequest { Model = _model, Input = text };

        var response = await _http.PostAsJsonAsync("/api/embed", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: ct);

        if (result?.Embeddings is null || result.Embeddings.Count == 0)
            throw new InvalidOperationException("Ollama returned no embeddings.");

        return result.Embeddings[0];
    }

    public void Dispose() => _http.Dispose();
}
