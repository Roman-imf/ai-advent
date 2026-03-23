using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;


namespace ClaudeCli;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Получаем API ключ из переменных окружения
            var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine(
                    "Ошибка: API ключ не найден. Укажите его в переменной окружения ANTHROPIC_API_KEY");
                return;
            }

            // Парсим аргументы командной строки
            var (message, model, maxTokens, temperature, instructionType) = ParseArguments(args);

            // Создаем клиент с API ключом из .env
            AnthropicClient client = new() { ApiKey = apiKey };

            // Получаем параметры из .env или используем значения по умолчанию
            var defaultModel = Environment.GetEnvironmentVariable("CLAUDE_MODEL") ?? "haiku";
            var defaultMaxTokens = int.Parse(Environment.GetEnvironmentVariable("CLAUDE_MAX_TOKENS") ?? "1024");
            var defaultTemperature = double.Parse(Environment.GetEnvironmentVariable("CLAUDE_TEMPERATURE") ?? "0,7");

            // Используем переданные аргументы или значения по умолчанию
            var selectedModel = model ?? defaultModel;
            var selectedMaxTokens = maxTokens ?? defaultMaxTokens;
            var selectedTemperature = temperature ?? defaultTemperature;

            message = instructionType switch
            {
                InstructionType.Aggressive => $"{message}. Сформулируй ответ максимально коротко и даже агрессивно",
                InstructionType.Polite =>
                    $"{message}. В ответе будь максимально вежлив, пиши детально не пропуская мелочей",
                null => message,
            };

            await SendSingleMessage(client, message, selectedModel, selectedMaxTokens, selectedTemperature);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }

    static (string? message, string? model, int? maxTokens, double? temperature, InstructionType? instructionType)
        ParseArguments(string[] args)
    {
        string? message = null;
        string? model = null;
        int? maxTokens = null;
        double? temperature = null;
        InstructionType? instructionType = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-m":
                case "--model":
                    if (i + 1 < args.Length)
                        model = args[++i];
                    break;

                case "-t":
                case "--tokens":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int tokens))
                        maxTokens = tokens;
                    break;


                case "-temp":
                case "--temperature":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double temp))
                        temperature = temp;
                    break;

                case "-h":
                case "--help":
                    ShowHelp();
                    Environment.Exit(0);
                    break;

                case "-i":
                case "--instructions":
                    instructionType = (InstructionType)int.Parse(args[++i]);
                    break;

                default:
                    // Если аргумент не начинается с "-", считаем его сообщением
                    if (!args[i].StartsWith("-"))
                    {
                        if (string.IsNullOrEmpty(message))
                            message = args[i];
                        else
                            message += " " + args[i];
                    }

                    break;
            }
        }

        return (message, model, maxTokens, temperature, instructionType);
    }

    static void ShowHelp()
    {
        Console.WriteLine("Использование: dotnet run [опции] [сообщение]");
        Console.WriteLine();
        Console.WriteLine("Опции:");
        Console.WriteLine("  -m, --model MODEL       Выбрать модель Claude (по умолчанию: claude-3-haiku-20240307)");
        Console.WriteLine("  -t, --tokens N          Установить максимальное количество токенов");
        Console.WriteLine("  -temp, --temperature N  Установить температуру (0-1)");
        Console.WriteLine("  -i, --interactive       Запустить интерактивный режим");
        Console.WriteLine("  -h, --help              Показать эту справку");
        Console.WriteLine();
        Console.WriteLine("Примеры:");
        Console.WriteLine("  dotnet run \"Привет, как дела?\"");
        Console.WriteLine("  dotnet run -m claude-3-opus-20240229 -temp 0.5 \"Напиши стих\"");
        Console.WriteLine("  dotnet run -i");
        Console.WriteLine();
        Console.WriteLine("Переменные окружения (.env файл):");
        Console.WriteLine("  ANTHROPIC_API_KEY=ваш-ключ");
        Console.WriteLine("  CLAUDE_MODEL=claude-3-haiku-20240307");
        Console.WriteLine("  CLAUDE_MAX_TOKENS=1024");
        Console.WriteLine("  CLAUDE_TEMPERATURE=0.7");
    }

    static async Task SendSingleMessage(AnthropicClient client, string message, string model, int maxTokens,
        double temperature)
    {
        var models = new Dictionary<string, string>
        {
            { "haiku", "claude-haiku-4-5" },
            { "sonnet", "claude-sonnet-4-5" },
            { "opus", "claude-opus-4-6" },
        };

        Console.WriteLine($"Отправка запроса к модели: {models[model]}");
        Console.WriteLine($"Макс. токенов: {maxTokens}, Температура: {temperature}");
        Console.WriteLine($"Текст: {message}");
        Console.WriteLine(new string('-', 50));

        MessageCreateParams parameters = new()
        {
            MaxTokens = maxTokens,
            Temperature = temperature,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = message,
                },
            ],
            Model = models[model],
        };

        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var response = await client.Messages.Create(parameters);
            sw.Stop();
            Console.WriteLine("Ответ Claude:");
            Console.WriteLine(new string('-', 50));
            var a = JsonSerializer.Deserialize<ClaudeResponse>(response.Content.First().Json.GetRawText());
            Console.WriteLine(a.Text);
            Console.WriteLine(new string('-', 50));
            var costs = new Dictionary<string, (decimal Input, decimal Output)>
            {
                { "haiku", (Input: 1 / (decimal)1000000, Output: 5 / (decimal)1000000) },
                { "sonnet", (Input: 3 / (decimal)1000000, Output: 15 / (decimal)1000000) },
                { "opus", (Input: 5 / (decimal)1000000, Output: 25 / (decimal)1000000) }
            };
            Console.WriteLine(
                $"Время запроса {sw.Elapsed}, " +
                $"Входящие токены: {response.Usage.InputTokens}, " +
                $"Исходящие токены: {response.Usage.OutputTokens}, " +
                $"Стоимость: {(response.Usage.InputTokens * costs[model].Input) + (response.Usage.OutputTokens * costs[model].Output)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при запросе к API: {ex.Message}");
        }
    }
}

internal enum InstructionType
{
    Polite = 1,
    Aggressive = 2
}

public class ClaudeResponse
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("text")] public string Text { get; set; }
}