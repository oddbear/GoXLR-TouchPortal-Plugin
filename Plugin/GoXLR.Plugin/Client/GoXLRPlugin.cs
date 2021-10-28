using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoXLR.Server;
using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace GoXLR.Plugin.Client
{
    public class GoXLRPlugin : ITouchPortalEventHandler
    {
        public string PluginId => "oddbear.touchportal.goxlr";

        private readonly ITouchPortalClient _client;
        private readonly GoXLRServer _server;
        private readonly ILogger<GoXLRPlugin> _logger;
        //The issue with to many updates performance is towards TP, so this is where the filter should be.
        private readonly Dictionary<string, bool> _stateTracker = new();

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

            _server.UpdateSelectedProfileEvent += profileName =>
            {
                _client.StateUpdate(PluginId + ".state.selectedProfile", profileName ?? "");
            };

            _server.UpdateRoutingEvent += () =>
            {
                var routingStates = _server.RoutingStates;
                if (routingStates is null)
                    return;
                
                foreach (var state in routingStates)
                {
                    var key = state.Key;
                    if (string.IsNullOrEmpty(key) || key.Length < 1)
                        continue;

                    var parts = key.Split("|");
                    parts[0] = char.ToLowerInvariant(parts[0][0]) + parts[0].Substring(1).Replace(" ", "");
                    parts[1] = char.ToLowerInvariant(parts[1][0]) + parts[1].Substring(1).Replace(" ", "");

                    //TODO: Fix broken states, all in Samples column is broken now:
                    var stateId = PluginId + ".state.routing." + parts[0] + "." + parts[1];
                    if (_stateTracker.ContainsKey(stateId) && _stateTracker[stateId] == state.Value)
                        continue;

                    _stateTracker[stateId] = state.Value;
                    _client.StateUpdate(stateId, state.Value ? "On" : "Off");
                }
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
            UpdateClientState();
        }

        /// <summary>
        /// Event fired when selecting a item from the dropdown in the TP Configurator.
        /// Updates a second list (instanceId) with the values from the selected client name/ip.
        /// </summary>
        /// <param name="message"></param>
        public void OnListChangedEvent(ListChangeEvent message)
        {
            try
            {
                //Choice is changed: I can now update the next list:
                _logger.LogInformation($"Choice Event: {message.ListId}'.");

                if (string.IsNullOrWhiteSpace(message.InstanceId))
                    return;

                //Profiles client selected, fetch profiles for client:
                if (message.ActionId.EndsWith(".profiles.action.change") &&
                    message.ListId.EndsWith(".profiles.action.change.data.clients"))
                {
                    var profiles = _server.ClientData?.Profiles;
                    if (profiles is null)
                        return;
                    
                    _client.ChoiceUpdate(PluginId + ".profiles.action.change.data.profiles", profiles, message.InstanceId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
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
        public void UpdateClientState()
        {
            try
            {
                var clientData = _server.ClientData;
                if (clientData is null)
                    return;

                var client = clientData.ClientIdentifier.ClientIpAddress;

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

            dict.TryGetValue(name + ".clients", out var clientIp);
            
            var input = dict[name + ".inputs"];
            var output = dict[name + ".outputs"];
            var action = dict[name + ".actions"];

            _server.SetRouting(action, input, output);
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
