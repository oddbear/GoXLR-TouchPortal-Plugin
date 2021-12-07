using GoXLR.Server.Models;
using GoXLR.TouchPortal.Plugin.Client;
using Lamar;
using TouchPortalSDK.Interfaces;

namespace GoXLR.Server.Configuration
{
    public class GoXLRPluginRegistry : ServiceRegistry
	{
		public GoXLRPluginRegistry()
        {
            //Both ITouchPortalClient and IGoXLREventHandler is dependent on GoXLRPlugin.
            //This means none of theese can be injected into the GoXLRPlugin, without special configuration.
            ForConcreteType<GoXLRPlugin>().Configure.Singleton();

            //Dependent on Plugin:
            For<ITouchPortalClient>().Use(context => {
                var plugin = context.GetInstance<GoXLRPlugin>();
                var clientFactory = context.GetInstance<ITouchPortalClientFactory>();

                return clientFactory.Create(plugin);
            }).Singleton();

            //Dependend on Plugin:
            For<IGoXLREventHandler>().Use<GoXLREventHandler>().Singleton();
        }
	}
}
