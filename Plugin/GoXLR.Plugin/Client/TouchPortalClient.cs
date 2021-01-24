using System;
using System.Collections.Generic;
using System.Linq;
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
        
        private ClientIdentifier[] Clients = Array.Empty<ClientIdentifier>();

        private IMessageProcessor _messageProcessor;
        private Task _listener;

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
            
            _messageProcessor.OnConnectEventHandler += () => _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
            _messageProcessor.OnCloseEventHandler += () => _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");
            _messageProcessor.OnListChangeEventHandler += OnListChangeEventHandler;
            _messageProcessor.OnActionEvent += OnActionEvent;

            //Connecting to TouchPortal:
            await _messageProcessor.TryPairAsync();

            //TODO: Why does this hold forever?
            _listener = Task.Run(async () =>
            {
                await _messageProcessor.Listen();
                _logger.LogError("Listener done?");
                Console.WriteLine("------------- !!! ---------------");
            });
        }
        
        public void UpdateClientState(ClientIdentifier[] profilesIdentifiers)
        {
            Clients = profilesIdentifiers;
            
            //Since ports are quite random, we only use the ip when connecting to the plugin.
            //There is only possible (without faking it) to have one client per ip.
            //Therefor this is a unique identifier that will hold between restarts.
            var clients = profilesIdentifiers
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

        /// <summary>
        /// Event fired when selecting a item from the dropdown in the TP Configurator.
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="listId"></param>
        /// <param name="instanceId"></param>
        /// <param name="value"></param>
        private void OnListChangeEventHandler(string actionId, string listId, string instanceId, string value)
        {
            //Choice is changed: I can now update the next list:
            _logger.LogInformation($"Choice Event: {nameof(actionId)}: '{actionId}', {nameof(listId)}: '{listId}', {nameof(instanceId)}: '{instanceId}', {nameof(value)}: '{value}'.");
            
            if(string.IsNullOrWhiteSpace(instanceId))
                return;

            //Profiles client selected, fetch profiles for client:
            if (actionId == ns + ".multiple.profiles.action.change" &&
                listId   == ns + ".multiple.profiles.action.change.data.clients")
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
        
        /// <summary>
        /// On a button press on the Touch interface client.
        /// </summary>
        /// <param name="actionId">Id of the action</param>
        /// <param name="datalist">Data list of the action</param>
        private void OnActionEvent(string actionId, List<ActionData> datalist)
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
            if (string.IsNullOrWhiteSpace(clientIp) || clientIp == "default")
            {
                //TODO: better logic:
                return Clients.FirstOrDefault();
            }
            
            return Clients
                .FirstOrDefault(identifier => identifier.ClientIpAddress == clientIp);
        }
    }
}
