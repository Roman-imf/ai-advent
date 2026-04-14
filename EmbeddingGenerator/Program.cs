using System.Text.Json;
using EmbeddingGenerator;

// ── Configuration ──────────────────────────────────────────────────────────────
const string ollamaModel = "nomic-embed-text";
const string ollamaUrl = "http://localhost:11434";
int chunkSize = 100; // tokens per chunk
int chunkOverlap = 20; // tokens shared between adjacent chunks

// ── Sample text ────────────────────────────────────────────────────────────────
// Replace this with File.ReadAllText("your-file.txt") to process an external file
var text = """
           Artificial intelligence (AI) is intelligence demonstrated by machines, as opposed to
           natural intelligence displayed by animals including humans. AI research has been defined
           as the field of study of intelligent agents, which refers to any system that perceives
           its environment and takes actions that maximize its chance of achieving its goals.

           Machine learning is a subset of artificial intelligence that gives systems the ability
           to automatically learn and improve from experience without being explicitly programmed.
           Machine learning focuses on the development of computer programs that can access data
           and use it to learn for themselves.

           Deep learning is part of a broader family of machine learning methods based on artificial
           neural networks with representation learning. Learning can be supervised, semi-supervised
           or unsupervised. Deep learning architectures such as deep neural networks, recurrent neural
           networks, convolutional neural networks and transformers have been applied to fields
           including computer vision, speech recognition, natural language processing, and more.

           Natural language processing (NLP) is a subfield of linguistics, computer science, and
           artificial intelligence concerned with the interactions between computers and human language,
           in particular how to program computers to process and analyze large amounts of natural
           language data. The goal is a computer capable of understanding the contents of documents,
           including the contextual nuances of the language within them.
           """;

// ── Step 1: Tokenize & chunk ───────────────────────────────────────────────────
await F("embeddings_overlap.json");
chunkSize = 500;
chunkOverlap = 0;
await F("embeddings_no_overlap.json");

async Task F(string outputFile)
{
    Console.WriteLine("Tokenizing and chunking text...");
    var chunker = new TextChunker(chunkSize, chunkOverlap);
    var chunks = chunker.Chunk(text);
    var allTokens = chunker.Tokenize(text);

    Console.WriteLine($"  Total tokens : {allTokens.Count}");
    Console.WriteLine($"  Total chunks : {chunks.Count}");
    Console.WriteLine();

// ── Step 2: Generate embeddings ────────────────────────────────────────────────
    Console.WriteLine($"Generating embeddings with '{ollamaModel}'...");

    var progress = new Progress<(int done, int total)>(p =>
        Console.Write($"\r  [{p.done}/{p.total}] chunk {p.done} embedded..."));

    try
    {
        using var embeddingService = new OllamaEmbeddingService(ollamaModel, ollamaUrl);
        await embeddingService.EmbedChunksAsync(chunks, progress);
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"\nCould not reach Ollama at {ollamaUrl}.");
        Console.Error.WriteLine($"Make sure Ollama is running and '{ollamaModel}' is pulled.");
        Console.Error.WriteLine($"Details: {ex.Message}");
        return;
    }

    Console.WriteLine($"\n  Done. Each embedding has {chunks[0].EmbeddingDimensions} dimensions.");
    Console.WriteLine();

// ── Step 3: Build document & save to JSON ──────────────────────────────────────
    var document = new EmbeddingDocument
    {
        SourceTextPreview = text.Length > 200 ? text[..200].Trim() + "..." : text.Trim(),
        Model = ollamaModel,
        GeneratedAt = DateTime.UtcNow,
        TotalTokens = allTokens.Count,
        ChunkSize = chunkSize,
        ChunkOverlap = chunkOverlap,
        TotalChunks = chunks.Count,
        Chunks = chunks
    };

    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(document, jsonOptions);

    await File.WriteAllTextAsync(outputFile, json);

    Console.WriteLine($"Embeddings saved to: {Path.GetFullPath(outputFile)}");
    Console.WriteLine();

// ── Step 4: Preview ─────────────────────────────────────────────────────────────
    Console.WriteLine("Preview of first chunk:");
    Console.WriteLine($"  Text        : {chunks[0].Text[..Math.Min(80, chunks[0].Text.Length)]}...");
    Console.WriteLine($"  Token count : {chunks[0].TokenCount}");
    Console.WriteLine(
        $"  Embedding   : [{string.Join(", ", chunks[0].Embedding.Take(5).Select(v => v.ToString("F4")))} ...]");
}