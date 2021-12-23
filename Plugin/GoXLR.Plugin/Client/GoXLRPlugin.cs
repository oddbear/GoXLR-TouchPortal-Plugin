using System;
using System.Threading.Tasks;
using GoXLR.Server;
using GoXLR.TouchPortal.Plugin.Configuration;
using GoXLR.TouchPortal.Plugin.Models;
using Microsoft.Extensions.Logging;
using TouchPortalSDK.Messages.Events;

namespace GoXLR.TouchPortal.Plugin.Client
{
    /// <summary>
    /// The plugin to communicate to and from Touch Portal.
    /// Most state updates happens through the GoXLREventHandler.
    /// </summary>
    public class GoXLRPlugin : TouchPortalEventHandler
    {
        private readonly GoXLRServer _server;
        private readonly ILogger<GoXLRPlugin> _logger;

        public GoXLRPlugin(
            GoXLRServer server,
            ILogger<GoXLRPlugin> logger)
            : base(server, logger)
        {
            _server = server;
            _logger = logger;
        }
        
        /// <summary>
        /// This is triggered when a user presses a button on their Android or iOS device.
        /// </summary>
        /// <param name="message"></param>
        public override async Task OnActionEvent(ActionEvent message)
        {
            try
            {
                _logger.LogInformation($"Action Event: {message.ActionId}");

                switch (message.ActionId)
                {
                    //Routing change:
                    case Identifiers.RoutingTableChangeRequestedId
                        when RouteChangeModel.TryParse(message, out var routeChange):

                        await _server.SetRouting(routeChange.RoutingAction, routeChange.Routing, base.Token);

                        break;

                    //Profile change:
                    case Identifiers.ProfileChangeRequestedId
                        when ProfileChangeModel.TryParse(message, out var profileChange):

                        await _server.SetProfile(profileChange.Profile, base.Token);

                        break;

                    default:
                        _logger.LogError($"No know action '{message.ActionId}' or data was corrupted.");
                        break;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error when processing '{message.ActionId}'");
            }
        }
    }
}
