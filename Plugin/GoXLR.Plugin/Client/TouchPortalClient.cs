using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoXLR.Shared;
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

        private IMessageProcessor _messageProcessor;
        private Task _listener;

        public TouchPortalClient(ILogger<TouchPortalClient> logger,
            Task<IMessageProcessor> messageProcessorFactory,
            GoXLRServer server)
        {
            _logger = logger;
            _messageProcessorFactory = messageProcessorFactory;
            _server = server;

            _server.FetchedProfilesEvent = (message) =>
            {
                UpdateProfiles(message.InstanceId, message.Profiles);
            };
            _server.UpdateConnectedClientsEvent = identifiers =>
            {
                var clientIps = identifiers
                    .Select(client => client.ClientIpAddress)
                    .ToArray();

                UpdateClientState(clientIps);
            };
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

        public void UpdateProfiles(string instanceId, string[] profiles)
        {
            _messageProcessor.UpdateChoice(new ChoiceUpdate
            {
                Id = ns + ".multiple.profiles.action.change.data.profiles",
                InstanceId = instanceId,
                Value = profiles
            });
        }

        public void UpdateClientState(string[] clients)
        {
            if(clients is null)
                return;

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
                var clientIp = value;
                if (clientIp == "default")
                    clientIp = GetDefaultClient();

                _server.FetchProfiles(clientIp, instanceId);
            }
        }

        private string GetDefaultClient()
        {
            //TODO: Find some logic for this one.
            return "127.0.0.1";
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

            if (!dict.TryGetValue(name + ".clients", out var clientIp) ||
                clientIp == "default")
            {
                clientIp = GetDefaultClient();
            }
            
            var input = dict[name + ".inputs"];
            var output = dict[name + ".outputs"];
            var action = dict[name + ".actions"];

            _server.SetRouting(clientIp, action, input, output);
        }

        private void ProfileChange(string name, List<ActionData> datalist)
        {
            var dict = datalist
                .ToDictionary(kv => kv.Id, kv => kv.Value);

            if (!dict.TryGetValue(name + ".clients", out var clientIp) ||
                clientIp == "default")
            {
                clientIp = GetDefaultClient();
            }
            
            var profile = dict[name + ".profiles"];

            _server.SetProfile(clientIp, profile);
        }
    }
}
