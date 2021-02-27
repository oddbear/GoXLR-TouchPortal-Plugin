using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using GoXLR.Plugin.Models;
using GoXLR.Server;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;

namespace GoXLR.Plugin.Client
{
    public class TouchPortalClient
    {
        private readonly ILogger<TouchPortalClient> _logger;
        private readonly GoXLRServer _server;
        private readonly MessageProcessor _messageProcessor;
        private readonly IReadOnlyCollection<string> _localAddresses;
        
        public TouchPortalClient(ILogger<TouchPortalClient> logger,
            GoXLRServer server,
            MessageProcessor messageProcessor)
        {
            _logger = logger;
            _server = server;
            _messageProcessor = messageProcessor;
            _localAddresses = GetLocalAddresses();

            //Set the event handler for GoXLR Clients connected:
            _server.UpdateConnectedClientsEvent = UpdateClientState;
            _messageProcessor.OnInfo = (infoMessage) =>
            {
                _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
                UpdateClientState();
            };
            _messageProcessor.OnDisconnect = (exception) =>
            {
                _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");
                Environment.Exit(0);
            };
            _messageProcessor.OnListChange = OnListChangeEventHandler;
            _messageProcessor.OnActionEvent = OnActionEvent;

        }

        public void Init()
        {
            //Connecting to TouchPortal:
            _messageProcessor.Connect();
        }

        /// <summary>
        /// Updates the clients connected, and the state of the clients, ex. profiles.
        /// </summary>
        public void UpdateClientState()
        {
            if (_messageProcessor is null)
            {
                _logger.LogWarning("MessageProcess not Initialized, but a client was connected.");
                return;
            }

            try
            {
                //Since ports are quite random, we only use the ip when connecting to the plugin.
                //There is only possible (without faking it) to have one client per ip.
                //Therefor this is a unique identifier that will hold between restarts.
                var clients = _server.ClientData
                    .Select(clientData => clientData.ClientIdentifier)
                    .Select(identifier => identifier.ClientIpAddress)
                    .ToArray();

                var clientChoices = new List<string> { "default" };
                clientChoices.AddRange(clients);

                //Update states:
                _messageProcessor.UpdateState(".single.clients.state.connected", clients.FirstOrDefault() ?? "none");
                _messageProcessor.UpdateState(".multiple.clients.states.count",clients.Length.ToString());

                //Update choices:
                _messageProcessor.UpdateChoice(".multiple.routingtable.action.change.data.clients", clientChoices.ToArray());

                _messageProcessor.UpdateChoice(".multiple.profiles.action.change.data.clients", clientChoices.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Event fired when selecting a item from the dropdown in the TP Configurator.
        /// Updates a second list (instanceId) with the values from the selected client name/ip.
        /// </summary>
        /// <param name="listChange"></param>
        private void OnListChangeEventHandler(ListChangeMessage listChange)
        {
            try
            {
                //Choice is changed: I can now update the next list:
                _logger.LogInformation($"Choice Event: {listChange}'.");

                if (string.IsNullOrWhiteSpace(listChange.InstanceId))
                    return;

                //Profiles client selected, fetch profiles for client:
                if (listChange.ActionId.EndsWith(".multiple.profiles.action.change") &&
                    listChange.ListId.EndsWith(".multiple.profiles.action.change.data.clients"))
                {
                    var client = GetClients(listChange.Value);
                    if (client is null)
                        return;

                    var clientData = GetClients(listChange.Value);
                    if (clientData is null)
                        return;

                    _messageProcessor.UpdateChoice(".multiple.profiles.action.change.data.profiles", clientData.Profiles, listChange.InstanceId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// On a button press on the Touch interface client.
        /// </summary>
        /// <param name="action"></param>
        private void OnActionEvent(ActionMessage action)
        {
            try
            {
                _logger.LogInformation($"Action Event: {action}");

                var actionId = action.ActionId;

                //Routing change:
                if (actionId.EndsWith(".routingtable.action.change"))
                {
                    //Can be both <pluginid>.<type>.routingtable.action.change,
                    // where <type> is single or multiple.
                    RouteChange(actionId + ".data", action.Data);
                }

                //Profile change:

                if (actionId.EndsWith(".profiles.action.change"))
                {
                    //Can be both <pluginid>.<type>.profiles.action.change,
                    // where <type> is single or multiple.
                    ProfileChange(actionId + ".data", action.Data);
                }
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
        private void RouteChange(string name, ActionData[] datalist)
        {
            var dict = datalist
                .ToDictionary(kv => kv.Id, kv => kv.Value);

            dict.TryGetValue(name + ".clients", out var clientIp);

            var client = GetClients(clientIp);
            if(client is null)
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
        private void ProfileChange(string name, ActionData[] datalist)
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
        /// <param name="clientIp"></param>
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

            //Try to find a exact match:
            return clients
                .FirstOrDefault(clientData => clientData.ClientIdentifier.ClientIpAddress == clientIp);
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
