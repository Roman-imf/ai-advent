using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class TimeTool
{
    [McpServerTool, Description("Возвращает текущее время на компьютере пользователя")]
    public string GetCurrentTime(
        [Description("Формат времени: 'short' (HH:mm), 'long' (HH:mm:ss), 'full' (дата + время). По умолчанию 'short'")]
        string format = "short")
    {
        return format switch
        {
            "short" => DateTime.Now.ToString("HH:mm"),
            "long"  => DateTime.Now.ToString("HH:mm:ss"),
            "full"  => DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            _       => $"Неверный формат '{format}'. Допустимые значения: 'short', 'long', 'full'."
        };
    }
}
