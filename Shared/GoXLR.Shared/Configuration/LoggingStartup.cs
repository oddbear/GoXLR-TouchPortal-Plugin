using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoXLR.Shared.Configuration
{
    //Not best practice, but I am lazy now.
    public static class LoggingStartup
    {
        public static void AddLogging(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(configure =>
            {
                configure.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ";
                });
            });
        }
    }
}
