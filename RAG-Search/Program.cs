using RagChatBot.Services;

// ── Configuration ──────────────────────────────────────────────────────────────
const string ollamaModel  = "nomic-embed-text";
const string ollamaUrl    = "http://localhost:11434";
const int    topChunks    = 3;          // how many RAG chunks to retrieve
const float  minScore     = 0.3f;       // minimum similarity score to include a chunk

// Path to the embeddings file produced by EmbeddingGenerator.
// Adjust to point at whichever file you want to search against.
string embeddingsPath = "embeddings_overlap.json";

// API key from environment variable
string apiKey = "";

// ── Startup ────────────────────────────────────────────────────────────────────
Console.WriteLine("RAG Chatbot");
Console.WriteLine("===========");
Console.WriteLine();

Console.WriteLine("Loading knowledge base...");
RagSearchService rag;
try
{
    rag = await RagSearchService.LoadAsync(embeddingsPath);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine("Run the EmbeddingGenerator project first to produce an embeddings file,");
    Console.Error.WriteLine($"then pass its path as the first argument:  dotnet run -- <path-to-embeddings.json>");
    return;
}

Console.WriteLine("Connecting to Ollama embedding service...");
using var ollama = new OllamaEmbeddingService(ollamaModel, ollamaUrl);

Console.WriteLine("Connecting to Claude API...");
var claude = new ClaudeService(apiKey);

Console.WriteLine();
Console.WriteLine("Ready! Type your message and press Enter.");
Console.WriteLine("Type 'exit' or 'quit' to stop.");
Console.WriteLine(new string('─', 60));

// ── Chat loop ──────────────────────────────────────────────────────────────────
while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("You: ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Goodbye!");
        break;
    }

    Console.WriteLine();

    try
    {
        // ── Step 1: Classify ─────────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Classifying message...");
        Console.ResetColor();

        bool isQuestion = await claude.IsQuestionAsync(input);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($" [{(isQuestion ? "question" : "not a question")}]");
        Console.ResetColor();

        string answer;

        if (isQuestion)
        {
            // ── Step 2: Embed query ──────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Searching knowledge base...");
            Console.ResetColor();

            float[] queryEmbedding;
            try
            {
                queryEmbedding = await ollama.GetEmbeddingAsync(input);
            }
            catch (HttpRequestException)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"  Could not reach Ollama at {ollamaUrl}. Make sure it is running.");
                continue;
            }

            // ── Step 3: Find closest chunks ──────────────────────────────────
            var results = rag.Search(queryEmbedding, topChunks)
                             .Where(r => r.Score >= minScore)
                             .ToList();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" {results.Count} chunk(s) found");
            Console.ResetColor();
            Console.WriteLine();

            if (results.Count > 0)
            {
                // ── Step 4: Print source chunks as block-quotes ──────────────
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Source chunks used:");
                Console.ResetColor();

                foreach (var r in results)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write($"  ┌─ Chunk #{r.Chunk.Index} (score: {r.Score:F3}) ");
                    Console.WriteLine(new string('─', Math.Max(0, 44 - r.Chunk.Index.ToString().Length)));
                    Console.ResetColor();

                    // Print text as a blockquote, indented with │
                    foreach (var line in WrapText(r.Chunk.Text, 56))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("  │ ");
                        Console.ResetColor();
                        Console.WriteLine(line);
                    }

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("  └" + new string('─', 56));
                    Console.ResetColor();
                    Console.WriteLine();
                }

                // ── Step 5: Ask Claude with context ──────────────────────────
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  Asking Claude...");
                Console.ResetColor();

                answer = await claude.AnswerWithContextAsync(input, results);
            }
            else
            {
                // No relevant chunks found — fall back to plain Claude
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No relevant chunks found, answering from general knowledge.");
                Console.ResetColor();
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  Asking Claude...");
                Console.ResetColor();

                answer = await claude.ChatAsync(input);
            }
        }
        else
        {
            // Not a question — just chat
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Asking Claude...");
            Console.ResetColor();

            answer = await claude.ChatAsync(input);
        }

        // ── Print Claude's answer ────────────────────────────────────────────
        Console.WriteLine(" done");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Claude: ");
        Console.ResetColor();
        Console.WriteLine(answer);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Error: {ex.Message}");
        Console.ResetColor();
    }
}

// ── Helpers ────────────────────────────────────────────────────────────────────

/// <summary>Wraps text at word boundaries to fit within <paramref name="width"/> characters.</summary>
static IEnumerable<string> WrapText(string text, int width)
{
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var line  = new System.Text.StringBuilder();

    foreach (var word in words)
    {
        if (line.Length > 0 && line.Length + 1 + word.Length > width)
        {
            yield return line.ToString();
            line.Clear();
        }
        if (line.Length > 0) line.Append(' ');
        line.Append(word);
    }

    if (line.Length > 0)
        yield return line.ToString();
}
