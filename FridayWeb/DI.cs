namespace FridayWeb.Services;

public static class DI
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IFridayChatService, FridayChatService>();
        return services;
    }
}