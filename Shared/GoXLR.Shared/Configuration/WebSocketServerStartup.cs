using GoXLR.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoXLR.Shared.Configuration
{
    public static class WebSocketServerStartup
    {
        public static void ConfigureWebSocketServer(IServiceCollection serviceCollection, IConfigurationRoot configurationRoot)
            => serviceCollection.Configure<WebSocketServerSettings>(configurationRoot.GetSection("WebSocketServerSettings"));

        public static void AddWebSocketServer(IServiceCollection serviceCollection)
            => serviceCollection.AddScoped<GoXLRServer>();
    }
}
