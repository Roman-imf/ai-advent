using System.Net.Http.Json;
using System.Text.Json;

namespace EmbeddingGenerator;

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

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(
            cancellationToken: ct
        );

        if (result?.Embeddings is null || result.Embeddings.Count == 0)
            throw new InvalidOperationException("Ollama returned no embeddings.");

        return result.Embeddings[0];
    }

    public async Task<List<TextChunk>> EmbedChunksAsync(
        List<TextChunk> chunks,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var embedding = await GetEmbeddingAsync(chunks[i].Text, ct);
            chunks[i].Embedding = embedding;
            chunks[i].EmbeddingDimensions = embedding.Length;

            progress?.Report((i + 1, chunks.Count));
        }

        return chunks;
    }

    public void Dispose() => _http.Dispose();
}
