using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class GreetingTool
{
    [McpServerTool, Description("Returns a greeting based on the time in the given datetime string")]
    public string GetGreeting(
        [Description("A datetime string, e.g. '2026-04-12 14:30:00' or '2026-04-12T08:00:00'")]
        string datetime)
    {
        if (!DateTime.TryParse(datetime, out var dt))
            return $"Invalid datetime format: '{datetime}'. Please use a format like '2026-04-12 14:30:00'.";

        return dt.Hour switch
        {
            >= 5 and < 12  => $"Good morning! 🌅 ({dt:HH:mm})",
            >= 12 and < 18 => $"Good afternoon! ☀️ ({dt:HH:mm})",
            >= 18 and < 22 => $"Good evening! 🌇 ({dt:HH:mm})",
            _              => $"Good night! 🌙 ({dt:HH:mm})"
        };
    }
}
