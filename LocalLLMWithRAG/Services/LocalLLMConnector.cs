using System.Net.Http.Json;
using System.Text.Json;

namespace LocalLLMWithRAG.Services;

public class LocalLLMConnector(HttpClient httpClient)
{
    private const string BaseUrl = "http://localhost:11434";
    private const string Model = "llama3.2:3b";

    public async Task<string> Generate(string prompt)
    {
        var request = new { model = Model, prompt, stream = false };
        var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/api/generate", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString() ?? string.Empty;
    }
}
