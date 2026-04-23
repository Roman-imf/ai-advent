using LocalLLMWithRAG.Models;

namespace LocalLLMWithRAG.Services;

public class ChatbotService(
    EmbeddingResult[] knowledgeBase,
    EmbeddingGenerator embeddingGenerator,
    LocalLLMConnector llmConnector)
{
    public async Task<string> Chat(string input)
    {
        var isQuestion = await CheckIsQuestion(input);

        if (!isQuestion)
            return await llmConnector.Generate(input);

        var inputEmbeddings = await embeddingGenerator.GenerateEmbedding([input]);
        var mostRelevant = FindMostRelevant(inputEmbeddings[0].Embedding);

        var prompt = $"""
            Answer the question using only the context below. Be concise.

            Context: {mostRelevant.Text}

            Question: {input}
            """;

        return await llmConnector.Generate(prompt);
    }

    private async Task<bool> CheckIsQuestion(string input)
    {
        var prompt = $"Is the following text a question? Reply only with 'yes' or 'no'.\n\nText: {input}";
        var response = await llmConnector.Generate(prompt);
        return response.Trim().StartsWith("yes", StringComparison.OrdinalIgnoreCase);
    }

    private EmbeddingResult FindMostRelevant(float[] queryEmbedding) =>
        knowledgeBase.MaxBy(e => CosineSimilarity(queryEmbedding, e.Embedding))!;

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var dot = a.Zip(b, (x, y) => x * y).Sum();
        var magA = MathF.Sqrt(a.Sum(x => x * x));
        var magB = MathF.Sqrt(b.Sum(x => x * x));
        return dot / (magA * magB);
    }
}
