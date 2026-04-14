using System.Text.RegularExpressions;

namespace EmbeddingGenerator;

public class TextChunker
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    // Matches words, numbers, and punctuation as individual tokens
    private static readonly Regex TokenPattern = new(@"\w+|[^\w\s]", RegexOptions.Compiled);

    public TextChunker(int chunkSize = 100, int overlap = 20)
    {
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than 0.");
        if (overlap < 0 || overlap >= chunkSize) throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be >= 0 and less than chunk size.");

        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return TokenPattern.Matches(text)
            .Select(m => m.Value)
            .ToList();
    }

    public List<TextChunk> Chunk(string text)
    {
        var tokens = Tokenize(text);
        var chunks = new List<TextChunk>();

        if (tokens.Count == 0)
            return chunks;

        int step = _chunkSize - _overlap;
        int index = 0;

        for (int start = 0; start < tokens.Count; start += step)
        {
            var chunkTokens = tokens.Skip(start).Take(_chunkSize).ToList();
            var chunkText = ReconstructText(chunkTokens);

            chunks.Add(new TextChunk
            {
                Index = index++,
                Text = chunkText,
                Tokens = chunkTokens,
                TokenCount = chunkTokens.Count
            });

            // Stop if we've covered all tokens
            if (start + _chunkSize >= tokens.Count)
                break;
        }

        return chunks;
    }

    // Rebuilds readable text from tokens, adding spaces between words
    // and no space before punctuation
    private static string ReconstructText(List<string> tokens)
    {
        if (tokens.Count == 0) return string.Empty;

        var result = new System.Text.StringBuilder(tokens[0]);

        for (int i = 1; i < tokens.Count; i++)
        {
            bool isPunct = tokens[i].Length == 1 && !char.IsLetterOrDigit(tokens[i][0]);
            if (!isPunct)
                result.Append(' ');
            result.Append(tokens[i]);
        }

        return result.ToString();
    }
}
