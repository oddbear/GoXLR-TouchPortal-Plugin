using System;
using System.Threading;
using GoXLR.Server;
using GoXLR.TouchPortal.Plugin.Configuration;
using GoXLR.TouchPortal.Plugin.Models;
using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Messages.Events;

namespace GoXLR.TouchPortal.Plugin.Client
{
    /// <summary>
    /// The plugin to communicate to and from Touch Portal.
    /// Most state updates happens through the GoXLREventHandler.
    /// </summary>
    public class GoXLRPlugin : ITouchPortalEventHandler
    {
        public string PluginId => Identifiers.Id;

        private readonly GoXLRServer _server;
        private readonly ILogger<GoXLRPlugin> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public GoXLRPlugin(
            GoXLRServer server,
            ILogger<GoXLRPlugin> logger)
        {
            _server = server;
            _logger = logger;
        }

        public void OnInfoEvent(InfoEvent message)
        {
            _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
        }

        public void OnClosedEvent(string message)
        {
            _cancellationTokenSource.Cancel();
            _server.Dispose();

            _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");
            Environment.Exit(0);
        }

        /// <summary>
        /// This is triggered when a user presses a button on their Android or iOS device.
        /// </summary>
        /// <param name="message"></param>
        public void OnActionEvent(ActionEvent message)
        {
            try
            {
                _logger.LogInformation($"Action Event: {message.ActionId}");
                
                switch (message.ActionId)
                {
                    //Routing change:
                    case Identifiers.RoutingTableChangeRequestedId
                        when RouteChangeModel.TryParse(message, out var routeChange):

                        _server.SetRouting(routeChange.RoutingAction, routeChange.Routing, _cancellationTokenSource.Token)
                            .GetAwaiter()
                            .GetResult();

                        break;

                    //Profile change:
                    case Identifiers.ProfileChangeRequestedId
                        when ProfileChangeModel.TryParse(message, out var profileChange):

                        _server.SetProfile(profileChange.Profile, _cancellationTokenSource.Token)
                            .GetAwaiter()
                            .GetResult();

                        break;

                    default:
                        _logger.LogError($"No know action '{message.ActionId}' or data was corrupted.");
                        break;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
            }
        }
        
        public void OnListChangedEvent(ListChangeEvent message)
        {
            //NotImplemented
        }

        public void OnBroadcastEvent(BroadcastEvent message)
        {
            //NotImplemented
        }

        public void OnSettingsEvent(SettingsEvent message)
        {
            //NotImplemented
        }

        public void OnUnhandledEvent(string jsonMessage)
        {
            //NotImplemented
        }
    }
}
