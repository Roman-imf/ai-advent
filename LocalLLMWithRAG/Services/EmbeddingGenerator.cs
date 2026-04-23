using System.Net.Http.Json;
using System.Text.Json;
using LocalLLMWithRAG.Models;

namespace LocalLLMWithRAG.Services;

public class EmbeddingGenerator(HttpClient httpClient)
{
    private const string BaseUrl = "http://localhost:11434";
    private const string Model = "nomic-embed-text";

    public async Task<EmbeddingResult[]> GenerateEmbedding(string[] texts)
    {
        var results = new EmbeddingResult[texts.Length];

        for (int i = 0; i < texts.Length; i++)
        {
            var request = new { model = Model, prompt = texts[i] };
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/api/embeddings", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var embedding = json.GetProperty("embedding")
                               .EnumerateArray()
                               .Select(e => e.GetSingle())
                               .ToArray();

            results[i] = new EmbeddingResult { Text = texts[i], Embedding = embedding };
        }

        return results;
    }
}
