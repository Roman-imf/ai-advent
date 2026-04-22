using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string OllamaUrl = "http://localhost:11434/api/generate";
const string Model = "llama3.2:3b";

// Собираем промпт из аргументов командной строки
if (args.Length == 0)
{
    Console.WriteLine("Использование: OllamaChat <ваш вопрос>");
    Console.WriteLine("Пример:        OllamaChat Напиши функцию сортировки на C#");
    return;
}

var prompt = string.Join(" ", args);

Console.WriteLine($"Модель : {Model}");
Console.WriteLine($"Вопрос : {prompt}");
Console.WriteLine(new string('─', 60));

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

var request = new OllamaRequest(Model, prompt, Stream: true);
var content = JsonContent.Create(request);

using var response = await http.PostAsync(OllamaUrl, content);

if (!response.IsSuccessStatusCode)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Ошибка: {response.StatusCode} — проверь, запущен ли Ollama (ollama serve)");
    Console.ResetColor();
    return;
}

// Читаем стриминговый ответ построчно
await using var stream = await response.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream, Encoding.UTF8);

while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(line)) continue;

    var chunk = JsonSerializer.Deserialize<OllamaResponse>(line);
    if (chunk is null) continue;

    Console.Write(chunk.Response); // печатаем токены по мере поступления

    if (chunk.Done) break;
}

Console.WriteLine();
Console.WriteLine(new string('─', 60));
Console.WriteLine("Готово.");

// ─── Модели запроса/ответа ──────────────────────────────────────

record OllamaRequest(
    [property: JsonPropertyName("model")]  string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("stream")] bool   Stream
);

record OllamaResponse(
    [property: JsonPropertyName("response")] string Response,
    [property: JsonPropertyName("done")]     bool   Done
);
