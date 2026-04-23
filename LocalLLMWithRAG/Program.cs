using LocalLLMWithRAG.Services;

var httpClient = new HttpClient();
var embeddingGenerator = new EmbeddingGenerator(httpClient);
var llmConnector = new LocalLLMConnector(httpClient);

// Knowledge base — replace with your own texts
var texts = new[]
{
    "The Eiffel Tower is located in Paris, France. It was built in 1889 by Gustave Eiffel.",
    "The Great Wall of China stretches over 13,000 miles and was built to protect Chinese states from invasions.",
    "The Amazon River is the largest river by discharge volume in the world, located in South America.",
};

Console.WriteLine("Generating embeddings for knowledge base...");
var knowledgeBase = await embeddingGenerator.GenerateEmbedding(texts);

var chatbot = new ChatbotService(knowledgeBase, embeddingGenerator, llmConnector);

Console.WriteLine("Chatbot ready. Type your message (or 'exit' to quit):\n");

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var response = await chatbot.Chat(input);
    Console.WriteLine($"Bot: {response}\n");
}
