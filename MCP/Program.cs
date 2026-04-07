using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpJavadocsClient;

public class McpJavadocsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private string? _sessionId;

    public McpJavadocsClient(string serverUrl)
    {
        _serverUrl = serverUrl;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task ConnectAsync()
    {
        Console.WriteLine("🔌 Подключение к MCP серверу Javadocs...");
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = 1,
            @params = new
            {
                protocolVersion = "0.1.0",
                capabilities = new
                {
                    roots = new { listChanged = true }
                }
            }
        };

        var response = await SendRequestAsync(request);
        
        if (response.TryGetProperty("result", out var result))
        {
            _sessionId = Guid.NewGuid().ToString();
            Console.WriteLine("✅ Подключение установлено!");
            Console.WriteLine($"   Сервер: {result.GetProperty("serverInfo").GetProperty("name").GetString()}");
            Console.WriteLine($"   Версия: {result.GetProperty("serverInfo").GetProperty("version").GetString()}");
            Console.WriteLine();
        }
    }

    public async Task<ListMcpToolsResponse> ListToolsAsync()
    {
        Console.WriteLine("📋 Получение списка доступных инструментов...");
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = 2,
            @params = new { }
        };

        var response = await SendRequestAsync(request);
        
        if (response.TryGetProperty("result", out var result))
        {
            var tools = JsonSerializer.Deserialize<List<McpTool>>(
                result.GetProperty("tools").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Console.WriteLine($"✅ Получено {tools?.Count ?? 0} инструментов\n");
            return new ListMcpToolsResponse { Tools = tools ?? new List<McpTool>() };
        }
        
        return new ListMcpToolsResponse { Tools = new List<McpTool>() };
    }

    public async Task<CallToolResponse> CallToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        Console.WriteLine($"🔧 Вызов инструмента: {toolName}");
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = 3,
            @params = new
            {
                name = toolName,
                arguments = arguments
            }
        };

        var response = await SendRequestAsync(request);
        
        if (response.TryGetProperty("result", out var result))
        {
            var content = result.GetProperty("content");
            var responseObj = JsonSerializer.Deserialize<CallToolResponse>(
                content.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return responseObj ?? new CallToolResponse();
        }
        
        return new CallToolResponse();
    }

    private async Task<JsonElement> SendRequestAsync(object request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Добавляем заголовки для SSE соединения
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _serverUrl);
        httpRequest.Content = content;
        
        // Добавляем session ID если есть
        if (!string.IsNullOrEmpty(_sessionId))
        {
            httpRequest.Headers.Add("X-Session-Id", _sessionId);
        }
        
        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        
        var responseText = await response.Content.ReadAsStringAsync();
        
        // Обработка SSE формата
        if (responseText.StartsWith("event: message"))
        {
            var lines = responseText.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    return JsonDocument.Parse(data).RootElement;
                }
            }
        }
        
        return JsonDocument.Parse(responseText).RootElement;
    }

    public void DisplayTools(List<McpTool> tools)
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("📚 ДОСТУПНЫЕ ИНСТРУМЕНТЫ MCP СЕРВЕРА");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        foreach (var tool in tools)
        {
            Console.WriteLine($"🔧 {tool.Name}");
            Console.WriteLine($"   Описание: {tool.Description ?? "Нет описания"}");
            
            if (tool.InputSchema != null && tool.InputSchema.TryGetValue("properties", out var properties))
            {
                Console.WriteLine("   Параметры:");
                var propsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(properties.ToString());
                
                if (propsDict != null)
                {
                    foreach (var prop in propsDict)
                    {
                        var type = prop.Value.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "unknown";
                        var description = prop.Value.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";
                        var required = false;
                        
                        if (tool.InputSchema.TryGetValue("required", out var requiredProp))
                        {
                            var requiredList = JsonSerializer.Deserialize<List<string>>(requiredProp.ToString());
                            required = requiredList?.Contains(prop.Key) ?? false;
                        }
                        
                        Console.WriteLine($"     • {prop.Key} ({type}){(required ? " [ОБЯЗАТЕЛЬНЫЙ]" : "")}");
                        if (!string.IsNullOrEmpty(description))
                        {
                            Console.WriteLine($"       {description}");
                        }
                    }
                }
            }
            Console.WriteLine();
        }
    }
}

// Модели данных
public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("inputSchema")]
    public Dictionary<string, JsonElement> InputSchema { get; set; } = new();
}

public class ListMcpToolsResponse
{
    public List<McpTool> Tools { get; set; } = new();
}

public class CallToolResponse
{
    [JsonPropertyName("content")]
    public List<ToolContent> Content { get; set; } = new();
}

public class ToolContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

// Основная программа
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        MCP JAVADOCS CLIENT - C# EXAMPLE                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");
        
        // URL публичного MCP сервера Javadocs для Spring Framework
        var serverUrl = "\nhttps://www.javadocs.dev/mcp";
        
        var client = new McpJavadocsClient(serverUrl);
        
        try
        {
            // 1. Подключаемся к серверу
            await client.ConnectAsync();
            
            // 2. Получаем список доступных инструментов
            var toolsResponse = await client.ListToolsAsync();
            
            // 3. Отображаем инструменты
            client.DisplayTools(toolsResponse.Tools);
            
            // 4. Пример вызова инструмента (если есть)
            if (toolsResponse.Tools.Any())
            {
                return;
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("💡 ПРИМЕР ИСПОЛЬЗОВАНИЯ");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                
                // Находим инструмент для поиска классов
                var searchTool = toolsResponse.Tools.FirstOrDefault(t => 
                    t.Name.Contains("search", StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("find", StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("class", StringComparison.OrdinalIgnoreCase));
                
                if (searchTool != null)
                {
                    Console.WriteLine($"🔍 Демонстрация вызова '{searchTool.Name}'...\n");
                    
                    // Пример аргументов для поиска
                    var args1 = new Dictionary<string, object>
                    {
                        ["query"] = "RestTemplate",
                        ["limit"] = 3
                    };
                    
                    var result = await client.CallToolAsync(searchTool.Name, args1);
                    
                    if (result.Content.Any())
                    {
                        Console.WriteLine("📄 Результат:");
                        foreach (var content in result.Content)
                        {
                            if (content.Type == "text" && !string.IsNullOrEmpty(content.Text))
                            {
                                Console.WriteLine($"   {content.Text}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Инструменты для поиска не найдены");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ Ошибка HTTP: {ex.Message}");
            Console.WriteLine("\n⚠️ Возможные причины:");
            Console.WriteLine("   • Сервер недоступен или требует авторизацию");
            Console.WriteLine("   • Неверный URL эндпоинта");
            Console.WriteLine("   • Сервер не поддерживает прямые HTTP запросы (требуется SSE)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка: {ex.Message}");
        }
        
        Console.WriteLine("\n✨ Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}