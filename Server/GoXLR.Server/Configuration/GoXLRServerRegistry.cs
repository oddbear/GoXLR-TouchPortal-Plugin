using Fleck;
using GoXLR.Server.Commands;
using GoXLR.Server.Handlers.Interfaces;
using Lamar;
using Microsoft.Extensions.DependencyInjection;

namespace GoXLR.Server.Configuration
{
    public class GoXLRServerRegistry : ServiceRegistry
	{
		public GoXLRServerRegistry()
		{
			//The server itself is a singleton, but it forks out scopes per connectetion (ex. GoXLRState):
			ForConcreteType<GoXLRServer>().Configure.Singleton();

			//The state is scoped to the connected client:
			ForConcreteType<CommandHandler>().Configure.Scoped();

			//Injectable, this means that we cannot know this before it is created.
			//The creating of a socket, starts a scope, and this scope will inject it for all services later.
			Injectable<IWebSocketConnection>();

			//Adds all the 
			Scan(scanner =>
			{
				scanner.Assembly(typeof(INotificationHandler).Assembly);
				scanner.AddAllTypesOf<INotificationHandler>(ServiceLifetime.Scoped);
			});
		}
	}
}
