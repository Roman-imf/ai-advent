namespace LocalLLMWithRAG.Models;

public class EmbeddingResult
{
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
}
