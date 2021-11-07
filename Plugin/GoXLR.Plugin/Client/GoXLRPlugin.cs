using System;
using System.Collections.Generic;
using System.Linq;
using GoXLR.Server;
using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace GoXLR.TouchPortal.Plugin.Client
{
    public class GoXLRPlugin : ITouchPortalEventHandler
    {
        public string PluginId => "oddbear.touchportal.goxlr";

        private readonly ITouchPortalClient _client;
        private readonly GoXLRServer _server;
        private readonly ILogger<GoXLRPlugin> _logger;
        //The issue with to many updates performance is towards TP, so this is where the filter should be.
        private readonly Dictionary<string, State> _stateTracker = new();

        public GoXLRPlugin(ITouchPortalClientFactory clientFactory,
            GoXLRServer goXLRServer,
            ILogger<GoXLRPlugin> logger)
        {
            //Set the event handler for TouchPortal:
            _client = clientFactory.Create(this);
            //Set the event handler for GoXLR connected:
            _server = goXLRServer;
            _logger = logger;

            //Set the event handler for GoXLR Clients connected:
            _server.UpdateConnectedClientsEvent = UpdateClientState;

            _server.UpdateProfilesEvent = profiles =>
            {
                var profileNames = profiles
                    .Select(profile => profile.Name)
                    .ToArray();

                _client.ChoiceUpdate(PluginId + ".profiles.action.change.data.profiles", profileNames);
            };

            _server.UpdateSelectedProfileEvent = profile =>
            {
                //TODO: State tracker?
                _client.StateUpdate(PluginId + ".state.selectedProfile", profile.Name ?? "");
            };

            _server.UpdateRoutingEvent = (routing, state) =>
            {
                if (routing is null)
                    return;
                
                //TODO: Fix broken states, all in Samples column is broken now:
                var input = routing.Input.GetEnumDescription();
                var output = routing.Output.GetEnumDescription();
                var stateId = $"{PluginId}.state:({input}|{output})";
                if (_stateTracker.ContainsKey(stateId) && _stateTracker[stateId] == state)
                    return;

                _stateTracker[stateId] = state;
                _client.StateUpdate(stateId, state.ToString());
            };
        }
        
        public void Init()
        {
            //Connecting to TouchPortal:
            _client.Connect();
        }

        public void OnInfoEvent(InfoEvent message)
        {
            _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
        }

        public void OnActionEvent(ActionEvent message)
        {
            try
            {
                _logger.LogInformation($"Action Event: {message.ActionId}");

                var actionId = message.ActionId;

                //Routing change:
                if (actionId.EndsWith(".routingtable.action.change"))
                {
                    //Can be both <pluginid>.<type>.routingtable.action.change,
                    // where <type> is single or multiple.
                    RouteChange(actionId + ".data", message.Data);
                }

                //Profile change:

                if (actionId.EndsWith(".profiles.action.change"))
                {
                    //Can be both <pluginid>.<type>.profiles.action.change,
                    // where <type> is single or multiple.
                    ProfileChange(actionId + ".data", message.Data);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        public void OnClosedEvent(string message)
        {
            _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");
            Environment.Exit(0);
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

        /// <summary>
        /// Updates the clients connected, and the state of the clients, ex. profiles.
        /// </summary>
        public void UpdateClientState(string client)
        {
            try
            {
                //Update states:
                _client.StateUpdate(PluginId + ".state.connectedClient", client);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Changes a route in the GoXLR app.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="datalist"></param>
        private void RouteChange(string name, IReadOnlyCollection<ActionDataSelected> datalist)
        {
            var dict = datalist
                .ToDictionary(kv => kv.Id, kv => kv.Value);
            
            //For these to be "parse able" to Routing class, we need to parse them from the Description.
            var input = dict[name + ".inputs"];
            var output = dict[name + ".outputs"];
            var action = dict[name + ".actions"];

            if (!Enum.TryParse<RoutingAction>(action, out var routingAction))
                return;

            if (!Routing.TryParseDescription(input, output, out var routing))
                return;
            
            _server.SetRouting(routingAction, routing);
        }

        /// <summary>
        /// Changes the profile in the GoXLR app.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="datalist"></param>
        private void ProfileChange(string name, IReadOnlyCollection<ActionDataSelected> datalist)
        {
            var dict = datalist
                .ToDictionary(kv => kv.Id, kv => kv.Value);
            
            var profile = dict[name + ".profiles"];

            _server.SetProfile(profile);
        }
    }
}
