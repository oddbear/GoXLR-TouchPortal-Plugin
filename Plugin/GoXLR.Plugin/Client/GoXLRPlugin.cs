using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using GoXLR.Server;
using GoXLR.Server.Models;
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
        private readonly IReadOnlyCollection<string> _localAddresses;

        public GoXLRPlugin(ITouchPortalClientFactory clientFactory,
            GoXLRServer goXLRServer,
            ILogger<GoXLRPlugin> logger)
        {
            //Set the event handler for TouchPortal:
            _client = clientFactory.Create(this);
            //Set the event handler for GoXLR connected:
            _server = goXLRServer;
            _logger = logger;
            _localAddresses = GetLocalAddresses();

            //Set the event handler for GoXLR Clients connected:
            _server.UpdateConnectedClientsEvent = UpdateClientState;
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
                if (message.ActionId.EndsWith(".multiple.profiles.action.change") &&
                    message.ListId.EndsWith(".multiple.profiles.action.change.data.clients"))
                {
                    var client = GetClients(message.Value);
                    if (client is null)
                        return;

                    var clientData = GetClients(message.Value);
                    if (clientData is null)
                        return;

                    _client.ChoiceUpdate(PluginId + ".multiple.profiles.action.change.data.profiles", clientData.Profiles, message.InstanceId);
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
                //Since ports are quite random, we only use the ip when connecting to the plugin.
                //There is only possible (without faking it) to have one client per ip.
                //Therefor this is a unique identifier that will hold between restarts.
                var clients = _server.ClientData
                    .Select(clientData => clientData.ClientIdentifier)
                    .Select(identifier => identifier.ClientIpAddress)
                    //Distinct since GoXLR right now does not close the old connection on reconnect:
                    .Distinct()
                    .ToArray();

                var clientChoices = new List<string> { "default" };
                clientChoices.AddRange(clients);

                //Update states:
                _client.StateUpdate(PluginId + ".single.clients.state.connected", clients.FirstOrDefault() ?? "none");
                _client.StateUpdate(PluginId + ".multiple.clients.states.count", clients.Length.ToString());

                //Update choices:
                _client.ChoiceUpdate(PluginId + ".multiple.routingtable.action.change.data.clients", clientChoices.ToArray());

                _client.ChoiceUpdate(PluginId + ".multiple.profiles.action.change.data.clients", clientChoices.ToArray());
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

            var client = GetClients(clientIp);
            if (client is null)
                return;

            var input = dict[name + ".inputs"];
            var output = dict[name + ".outputs"];
            var action = dict[name + ".actions"];

            _server.SetRouting(client.ClientIdentifier, action, input, output);
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

            dict.TryGetValue(name + ".clients", out var clientIp);

            var client = GetClients(clientIp);
            if (client is null)
                return;

            var profile = dict[name + ".profiles"];

            _server.SetProfile(client.ClientIdentifier, profile);
        }

        /// <summary>
        /// Get client data from a connected client name/ip.
        /// </summary>
        /// <param name="clientIp">Client value/ip from the TouchPortal dropdown.</param>
        /// <returns></returns>
        private ClientData GetClients(string clientIp)
        {
            var clients = _server.ClientData;

            //No exact IP is set:
            if (string.IsNullOrWhiteSpace(clientIp) || clientIp == "default")
            {
                //Try to use a local IP as client first:
                return clients.FirstOrDefault(clientData => _localAddresses.Contains(clientData.ClientIdentifier.ClientIpAddress))
                       //Or just give me the first one:
                       ?? clients.FirstOrDefault();
            }

            //Try to find a exact match (last is the most recent connection):
            return clients
                .LastOrDefault(clientData => clientData.ClientIdentifier.ClientIpAddress == clientIp);
        }

        /// <summary>
        /// Gets the ip addresses of current computer.
        /// </summary>
        /// <returns></returns>
        private IReadOnlyCollection<string> GetLocalAddresses()
        {
            var addresses = new List<string> { "127.0.0.1" };

            try
            {
                var hostName = Dns.GetHostName();
                addresses.Add(hostName);

                var host = Dns.GetHostEntry(hostName);
                var ipAddresses = host.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString());

                addresses.AddRange(ipAddresses);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return addresses.AsReadOnly();
        }
    }
}
