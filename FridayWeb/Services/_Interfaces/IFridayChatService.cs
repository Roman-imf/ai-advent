using FridayWeb.Entities;

namespace FridayWeb.Services;

public interface IFridayChatService
{
    public Task<FridayResponse> SendMessageAsync(string message);
    public Task SaveSettingsAsync(FridaySettings newSettings);
}