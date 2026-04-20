using System.Text.Json.Serialization;

namespace RagChatBot;

public class TextChunk
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("tokens")]
    public List<string> Tokens { get; set; } = [];

    [JsonPropertyName("token_count")]
    public int TokenCount { get; set; }

    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = [];

    [JsonPropertyName("embedding_dimensions")]
    public int EmbeddingDimensions { get; set; }
}

public class EmbeddingDocument
{
    [JsonPropertyName("source_text_preview")]
    public string SourceTextPreview { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("chunk_size")]
    public int ChunkSize { get; set; }

    [JsonPropertyName("chunk_overlap")]
    public int ChunkOverlap { get; set; }

    [JsonPropertyName("total_chunks")]
    public int TotalChunks { get; set; }

    [JsonPropertyName("chunks")]
    public List<TextChunk> Chunks { get; set; } = [];
}

// Ollama API request/response shapes
public class OllamaEmbedRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;
}

public class OllamaEmbedResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("embeddings")]
    public List<float[]> Embeddings { get; set; } = [];
}

public record SearchResult(TextChunk Chunk, float Score);
