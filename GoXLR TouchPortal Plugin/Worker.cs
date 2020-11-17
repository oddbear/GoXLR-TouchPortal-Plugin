//#define SKIP_WS

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TouchPortalApi.Interfaces;
using TouchPortalApi.Models;
using WatsonWebsocket;
using System.Linq;
using GoXLR_TouchPortal_Plugin.Models;
using System.Text.Json;

namespace GoXLR_TouchPortal_Plugin
{
    public class Worker : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<Worker> _logger;
        private readonly IMessageProcessor _messageProcessor;
#if !SKIP_WS
        private readonly WatsonWsServer _server;
#endif
        public Worker(IHostApplicationLifetime hostApplicationLifetime, ILogger<Worker> logger, IMessageProcessor messageProcessor)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
#if !SKIP_WS
            _server = new WatsonWsServer("127.0.0.1", 6805, false);
#endif
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopRequested = false;
#if !SKIP_WS
            // SetUp Server for GoXLR:
            _server.ClientConnected += async (sender, args) =>
            {
                //When GoXLR is connected, ask for Profiles.
                Console.WriteLine("Client connected: " + args.IpPort);
                _logger.LogInformation("Client connected: " + args.IpPort);

                try
                {
                    var model = GetProfilesRequest.Create();
                    var json = System.Text.Json.JsonSerializer.Serialize(model);

                    _logger.LogInformation(json);
                    await _server.SendAsync(args.IpPort, json, stoppingToken);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    _logger.LogError(e.ToString());
                }
            };

            _server.ClientDisconnected += (sender, args) =>
            {
                Console.WriteLine("Client disconnected: " + args.IpPort);
            };

            _server.MessageReceived += (sender, args) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(args.Data);
                    Console.WriteLine("Message received from " + args.IpPort + ": " + json);
                    _logger.LogInformation("Message received from " + args.IpPort);

                    var response = JsonSerializer.Deserialize<GetProfilesResponse>(json);

                    _messageProcessor.UpdateChoice(new ChoiceUpdate
                    {
                        Id = "tpgoxlr_profile_auto",
                        Value = response?.Payload.Profiles ?? new [] { "bah!" }
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _logger.LogError(e.ToString());
                }
            };

            _server.Start();
#endif
            //Setup Client for TouchPortal:

            // On Plugin Connect Event
            _messageProcessor.OnConnectEventHandler += () =>
            {
                Console.WriteLine($"{DateTime.Now} Plugin Connected to TouchPortal");
            };

            // On Action Event
            _messageProcessor.OnActionEvent += async (actionId, dataList) =>
            {
                Console.WriteLine($"{DateTime.Now} Action Event Fired: {actionId}");
#if !SKIP_WS

                try
                {
                    var clients = _server.ListClients();
                    var client = clients.SingleOrDefault();

                    if (client != null)
                    {
                        Console.WriteLine($"GoXLR Client found: {client}");

                        switch (actionId)
                        {
                            case "tpgoxlr_route_change":
                            {
                                var settings = GetRoutingSettingFromActionDataList(dataList);
                                var routingTable = Models.SetRoutingRequest.Create(settings);
                                var json = System.Text.Json.JsonSerializer.Serialize(routingTable);
                                await _server.SendAsync(client, json, stoppingToken);

                                break;
                            }
                            case "tpgoxlr_profile_change_manual":
                            case "tpgoxlr_profile_change_auto":
                            {
                                var profile = dataList.Single().Value;
                                var model = Models.SetProfileRequest.Create(profile);
                                var json = System.Text.Json.JsonSerializer.Serialize(model);
                                await _server.SendAsync(client, json, stoppingToken);

                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No GoXLR Clients connected. Restart the GoXLR App");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

#endif
                foreach (var actionData in dataList)
                {
                    Console.WriteLine($"Id: {actionData.Id} Value: {actionData.Value}");
                }
            };

            // On List Change Event
            _messageProcessor.OnListChangeEventHandler += (actionId, value) =>
            {
                Console.WriteLine($"{DateTime.Now} Choice Event Fired.");
            };

            // On Plugin Disconnect
            _messageProcessor.OnCloseEventHandler += () =>
            {
                Console.Write($"{DateTime.Now} Plugin Quit Command");
                stopRequested = true;
            };

            // Send State Update
            _messageProcessor.UpdateState(new StateUpdate {Id = "SomeStateId", Value = "New Value"});

            // Send Choice Update
            _messageProcessor.UpdateChoice(new ChoiceUpdate
            {
                Id = "tpgoxlr_profile_auto",
                Value = new[] { "No Value" }
            });

            // Run Listen and pairing
            _ = Task.WhenAll(new Task[]
            {
                _messageProcessor.Listen(),
                _messageProcessor.TryPairAsync()
            });

            try
            {
                // Do whatever you want in here
                while (!stoppingToken.IsCancellationRequested && !stopRequested)
                {
                    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);
                }
            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        }

        private static Models.SetRoutingRequest.SetRoutingSettings GetRoutingSettingFromActionDataList(IEnumerable<ActionData> actionData)
        {
            var setting = new Models.SetRoutingRequest.SetRoutingSettings();

            foreach (var data in actionData)
            {
                switch (data.Id)
                {
                    case "tpgoxlr_input":
                        setting.RoutingInput = data.Value;
                        break;
                    case "tpgoxlr_output":
                        setting.RoutingOutput = data.Value;
                        break;
                    case "tpgoxlr_action":
                        setting.RoutingAction = data.Value;
                        break;
                }
            }

            return setting;
        }
    }
}
