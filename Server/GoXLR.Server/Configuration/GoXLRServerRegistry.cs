using Fleck;
using GoXLR.Server.Handlers;
using Lamar;
using Microsoft.Extensions.DependencyInjection;

namespace GoXLR.Server.Configuration
{
    public class GoXLRServerRegistry : ServiceRegistry
	{
		public GoXLRServerRegistry()
		{
			ForConcreteType<GoXLRServer>().Configure.Singleton();
			ForConcreteType<Notifier>().Configure.Scoped();
			ForConcreteType<GoXLRState>().Configure.Scoped();

			Injectable<IWebSocketConnection>();

			Scan(scanner =>
			{
				scanner.Assembly(this.GetType().Assembly);
				scanner.AddAllTypesOf<INotificationHandler>(ServiceLifetime.Scoped);
			});
		}
	}
}
