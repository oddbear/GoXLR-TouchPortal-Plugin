using System;
using GoXLR.Server;
using GoXLR.TouchPortal.Plugin.Configuration;
using GoXLR.TouchPortal.Plugin.Models;
using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Messages.Events;

namespace GoXLR.TouchPortal.Plugin.Client
{
    public class GoXLRPlugin : ITouchPortalEventHandler
    {
        public string PluginId => Identifiers.Id;

        private readonly GoXLRServer _server;
        private readonly ILogger<GoXLRPlugin> _logger;

        public GoXLRPlugin(GoXLRServer goXLRServer,
            ILogger<GoXLRPlugin> logger)
        {
            //Set the event handler for GoXLR connected:
            _server = goXLRServer;
            _logger = logger;
        }

        public void OnInfoEvent(InfoEvent message)
        {
            _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
        }

        public void OnClosedEvent(string message)
        {
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

                        _server.SetRouting(routeChange.RoutingAction, routeChange.Routing);

                        break;

                    //Profile change:
                    case Identifiers.ProfileChangeRequestedId
                        when ProfileChangeModel.TryParse(message, out var profileChange):

                        _server.SetProfile(profileChange.Profile);

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
