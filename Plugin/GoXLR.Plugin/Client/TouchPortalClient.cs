using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GoXLR.Server;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;
using TouchPortalApi.Interfaces;
using TouchPortalApi.Models;

namespace GoXLR.Plugin.Client
{
    public class TouchPortalClient
    {
        private const string ns = "oddbear.touchportal.goxlr";

        private readonly ILogger<TouchPortalClient> _logger;
        private readonly Task<IMessageProcessor> _messageProcessorFactory;
        private readonly GoXLRServer _server;
        
        private ClientIdentifier[] _clients = Array.Empty<ClientIdentifier>();
        private IReadOnlyCollection<string> _localAddresses = Array.Empty<string>();

        private IMessageProcessor _messageProcessor;

        public TouchPortalClient(ILogger<TouchPortalClient> logger,
            Task<IMessageProcessor> messageProcessorFactory,
            GoXLRServer server)
        {
            _logger = logger;
            _messageProcessorFactory = messageProcessorFactory;
            _server = server;

            _server.UpdateConnectedClientsEvent = UpdateClientState;
        }

        public async Task InitAsync()
        {
            //Will wait until connected:
            _messageProcessor = await _messageProcessorFactory;

            _messageProcessor.OnConnectEventHandler += () =>
            {
                _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
                UpdateClientStateInitialized();
            };
            _messageProcessor.OnCloseEventHandler += () =>
            {
                _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");

                //This seems hackish, but is how the Java SDK does it.
                // or we can use method Set() on the ManualResetEvent at this point.
                // or we can check if the parent process exists.
                Environment.Exit(0);
            };
            _messageProcessor.OnListChangeEventHandler += OnListChangeEventHandler;
            _messageProcessor.OnActionEvent += OnActionEvent;
            
            _localAddresses = GetLocalAddresses();

            //Connecting to TouchPortal:
            _ = _messageProcessor.Listen();
            await _messageProcessor.TryPairAsync();

            UpdateClientStateInitialized();
        }
        
        public void UpdateClientState(ClientIdentifier[] profilesIdentifiers)
        {
            _clients = profilesIdentifiers;

            //TODO: Some issue after publish, but not in debug.
        }

        private void UpdateClientStateInitialized()
        {
            try
            {
                //Since ports are quite random, we only use the ip when connecting to the plugin.
                //There is only possible (without faking it) to have one client per ip.
                //Therefor this is a unique identifier that will hold between restarts.
                var clients = _clients
                    .Select(identifier => identifier.ClientIpAddress)
                    .ToArray();

                var clientChoices = new List<string> { "default" };
                clientChoices.AddRange(clients);

                //Update states:
                _messageProcessor.UpdateState(new StateUpdate
                {
                    Id = ns + ".single.clients.state.connected",
                    Value = clients.FirstOrDefault() ?? "none"
                });
                _messageProcessor.UpdateState(new StateUpdate
                {
                    Id = ns + ".multiple.clients.states.count",
                    Value = clients.Length.ToString()
                });

                //Update choices:
                _messageProcessor.UpdateChoice(new ChoiceUpdate
                {
                    Id = ns + ".multiple.routingtable.action.change.data.clients",
                    Value = clientChoices.ToArray()
                });

                _messageProcessor.UpdateChoice(new ChoiceUpdate
                {
                    Id = ns + ".multiple.profiles.action.change.data.clients",
                    Value = clientChoices.ToArray()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Event fired when selecting a item from the dropdown in the TP Configurator.
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="listId"></param>
        /// <param name="instanceId"></param>
        /// <param name="value"></param>
        private void OnListChangeEventHandler(string actionId, string listId, string instanceId, string value)
        {
            try
            {
                //Choice is changed: I can now update the next list:
                _logger.LogInformation($"Choice Event: {nameof(actionId)}: '{actionId}', {nameof(listId)}: '{listId}', {nameof(instanceId)}: '{instanceId}', {nameof(value)}: '{value}'.");

                if (string.IsNullOrWhiteSpace(instanceId))
                    return;

                //Profiles client selected, fetch profiles for client:
                if (actionId == ns + ".multiple.profiles.action.change" &&
                    listId == ns + ".multiple.profiles.action.change.data.clients")
                {
                    var client = GetClientIdentifier(value);
                    if (client is null)
                        return;

                    var clientData = _server.GetClientData(client);
                    if (clientData is null)
                        return;

                    _messageProcessor.UpdateChoice(new ChoiceUpdate
                    {
                        Id = ns + ".multiple.profiles.action.change.data.profiles",
                        InstanceId = instanceId,
                        Value = clientData.Profiles
                    });
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
        /// <param name="actionId">Id of the action</param>
        /// <param name="datalist">Data list of the action</param>
        private void OnActionEvent(string actionId, List<ActionData> datalist)
        {
            try
            {
                var dataValues = string.Join(", ", datalist.Select(kv => $"'{kv.Id}:{kv.Value}'"));
                _logger.LogInformation($"Action Event: {nameof(actionId)}: '{actionId}', {nameof(datalist)}: ({dataValues})");

                switch (actionId)
                {
                    //Routing change request with single client:
                    case ns + ".single.routingtable.action.change":
                        //Data:
                        RouteChange(ns + ".single.routingtable.action.change.data", datalist);
                        break;

                    //Routing change request with multiple clients:
                    case ns + ".multiple.routingtable.action.change":
                        //Data:
                        RouteChange(ns + ".multiple.routingtable.action.change.data", datalist);
                        break;

                    //Profile change request with single client:
                    case ns + ".single.profiles.action.change":
                        //Data:
                        ProfileChange(ns + ".single.profiles.action.change.data", datalist);
                        break;

                    //Profile change request with multiple clients:
                    case ns + ".multiple.profiles.action.change":
                        //Data:
                        ProfileChange(ns + ".multiple.profiles.action.change.data", datalist);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        private void RouteChange(string name, List<ActionData> datalist)
        {
            var dict = datalist
                .ToDictionary(kv => kv.Id, kv => kv.Value);

            dict.TryGetValue(name + ".clients", out var clientIp);

            var client = GetClientIdentifier(clientIp);
            if(client is null)
                return;
            
            var input = dict[name + ".inputs"];
            var output = dict[name + ".outputs"];
            var action = dict[name + ".actions"];

            _server.SetRouting(client, action, input, output);
        }

        private void ProfileChange(string name, List<ActionData> datalist)
        {
            var dict = datalist
                .ToDictionary(kv => kv.Id, kv => kv.Value);

            dict.TryGetValue(name + ".clients", out var clientIp);

            var client = GetClientIdentifier(clientIp);
            if (client is null)
                return;

            var profile = dict[name + ".profiles"];

            _server.SetProfile(client, profile);
        }

        private ClientIdentifier GetClientIdentifier(string clientIp)
        {
            //No exact IP is set:
            if (string.IsNullOrWhiteSpace(clientIp) || clientIp == "default")
            {
                //Try to use a local IP as client first:
                return _clients.FirstOrDefault(identifier => _localAddresses.Contains(identifier.ClientIpAddress))
                //Or just give me the first one:
                    ?? _clients.FirstOrDefault();
            }

            //Try to find a exact match:
            return _clients
                .FirstOrDefault(identifier => identifier.ClientIpAddress == clientIp);
        }

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
