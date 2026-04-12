using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<TimeTool>();
    })
    .Build();

await host.RunAsync();
