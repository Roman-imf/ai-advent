using System.Text.Json;

namespace RagChatBot.Services;

public class RagSearchService
{
    private readonly List<TextChunk> _chunks;

    private RagSearchService(List<TextChunk> chunks)
    {
        _chunks = chunks;
    }

    /// <summary>
    /// Loads an embedding document from a JSON file produced by EmbeddingGenerator.
    /// </summary>
    public static async Task<RagSearchService> LoadAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Embeddings file not found: {jsonPath}");

        var json = await File.ReadAllTextAsync(jsonPath);
        var doc = JsonSerializer.Deserialize<EmbeddingDocument>(json)
                  ?? throw new InvalidOperationException("Failed to deserialize embeddings file.");

        var chunksWithEmbeddings = doc.Chunks
            .Where(c => c.Embedding.Length > 0)
            .ToList();

        Console.WriteLine($"  Loaded {chunksWithEmbeddings.Count} chunks from '{Path.GetFileName(jsonPath)}'");
        return new RagSearchService(chunksWithEmbeddings);
    }

    /// <summary>
    /// Returns the top-N chunks closest to the query embedding using cosine similarity.
    /// </summary>
    public List<SearchResult> Search(float[] queryEmbedding, int topN = 3)
    {
        return _chunks
            .Select(c => new SearchResult(c, CosineSimilarity(queryEmbedding, c.Embedding)))
            .OrderByDescending(r => r.Score)
            .Take(topN)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        int len = Math.Min(a.Length, b.Length);
        float dot = 0f, normA = 0f, normB = 0f;

        for (int i = 0; i < len; i++)
        {
            dot   += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        float denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom < 1e-10f ? 0f : dot / denom;
    }
}
