using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Fleck;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoXLR.Server
{
    // ReSharper disable InconsistentNaming
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "This is they we write it.")]
    public class GoXLRServer
    {
        private readonly ILogger _logger;
        
        private readonly WebSocketServerSettings _settings;
        private readonly Dictionary<ClientIdentifier, IWebSocketConnection> _sockets = new();
        
        public List<ClientData> ClientData { get; } = new();

        public Action UpdateConnectedClientsEvent { get; set; }

        public Action<string> UpdateSelectedProfileEvent { get; set; }

        public Action UpdateRoutingEvent { get; set; }

        public Dictionary<string, bool> RoutingStates { get; }

        private readonly Thread _profileFetcherThread;

        public GoXLRServer(ILogger<GoXLRServer> logger,
            IOptions<WebSocketServerSettings> options)
        {
            _logger = logger;
            _settings = options.Value;

            RoutingStates = GetRoutingTable()
                .ToDictionary(routingKey => routingKey, _ => false);

            _profileFetcherThread = new Thread(FetchProfilesThreadSync) { IsBackground = true };
            _profileFetcherThread.Start();
        }

        private static string[] GetRoutingTable()
        {
            var inputs = new[] { "Mic", "Chat", "Music", "Game", "Console", "Line In", "System", "Samples" };
            var outputs = new[] { "Headphones", "Broadcast Mix", "Line Out", "Chat Mic", "Sampler" };

            var query =
                from input in inputs
                from output in outputs
                select $"{input}|{output}";

            return query.ToArray();
        }

        /// <summary>
        /// Starts the WebSockets server.
        /// </summary>
        public void Init()
        {
            var server = new WebSocketServer($"ws://{_settings.IpAddress}:{_settings.Port}/?GOXLRApp");
            server.Start(OnClientConnecting);
        }

        private void FetchProfilesThreadSync()
        {
            while (true)
            {
                try
                {
                    foreach (var socket in _sockets)
                    {
                        if (!socket.Value.IsAvailable)
                            continue;

                        //Get updated profiles:
                        FetchProfiles(socket.Key);
                    }
                }
                catch (Exception exception)
                {
                    _logger?.LogDebug(exception, "Something went wrong on the Listener Thread.");
                }

                Thread.Sleep(TimeSpan.FromMinutes(15));
            }
        }

        /// <summary>
        /// Setting up Fleck WebSocket callbacks.
        /// </summary>
        /// <param name="socket"></param>
        private void OnClientConnecting(IWebSocketConnection socket)
        {
            var connectionInfo = socket.ConnectionInfo;
            var identifier = new ClientIdentifier(connectionInfo.ClientIpAddress, connectionInfo.ClientPort);

            socket.OnOpen = () =>
            {
                _sockets.Add(identifier, socket);
                _logger.LogInformation($"Connection opened {socket.ConnectionInfo.ClientIpAddress}.");
            };

            socket.OnClose = () =>
            {
                _sockets.Remove(identifier);
                ClientData.RemoveAll(clientData => clientData.ClientIdentifier == identifier);

                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");
                
                UpdateConnectedClientsEvent?.Invoke();
            };

            socket.OnMessage = (message) =>
            {
                try
                {
                    _logger.LogWarning("Received message: " + message);

                    var document = JsonSerializer.Deserialize<JsonDocument>(message);
                    if (document is null)
                        return;

                    var root = document.RootElement;

                    var propertyAction = root.GetValue<string>("action");
                    var propertyEvent = root.GetValue<string>("event");
                    var propertyContext = root.GetValue<string>("context");

                    _logger.LogInformation($"Handling: {propertyEvent}");
                    switch (propertyEvent)
                    {
                        case "goxlrConnectionEvent":

                            ConnectedAndSubscribeToRoutingStates(identifier);
                            FetchProfiles(identifier);

                            break;

                        case "getSettings"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":

                            //We don't care about this "button". This is "global", and there is no state to ask for.
                            if (propertyContext == "fetchingProfiles")
                                break;

                            HandleProfileChangeSettingsEvent(identifier, propertyContext);

                            break;

                        case "getSettings"
                            when propertyAction == "com.tchelicon.goxlr.routingtable":

                            HandleRoutingTableSettingsEvent(identifier, propertyContext);

                            break;

                        case "setState"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":

                            var profileState = root
                                .GetProperty("payload")
                                .GetProperty("state")
                                .GetInt32();

                            if (profileState == 0)
                                UpdateSelectedProfileEvent?.Invoke(propertyContext);

                            break;

                        case "setState"
                            when propertyAction == "com.tchelicon.goxlr.routingtable":

                            var routingState = root
                                .GetProperty("payload")
                                .GetProperty("state")
                                .GetInt32();

                            RoutingStates[propertyContext] = routingState == 0;

                            UpdateRoutingEvent?.Invoke();

                            break;

                        case "sendToPropertyInspector"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":

                            //We only care about the event where we want to fetch the profiles.
                            //We can update TP on this, but this could be very noisy.
                            if (propertyContext != "fetchingProfiles")
                                break;

                            //Format:
                            var profiles = root
                                .GetProperty("payload")
                                .GetProperty("Profiles")
                                .EnumerateArray()
                                .Select(element => element.GetString())
                                .ToArray();

                            //TODO: Register new profiles
                            var oldData = ClientData
                                .FirstOrDefault(data => data.ClientIdentifier == identifier) ?? new ClientData(identifier, Array.Empty<string>());
                            
                            //Set client data:
                            var clientData = new ClientData(identifier, profiles);
                            ClientData.Add(clientData);

                            //Update client list, with data:
                            UpdateConnectedClientsEvent?.Invoke();

                            var profilesToRegister = profiles.Except(oldData.Profiles).ToArray();
                            SubscribeToProfileStates(identifier, profilesToRegister);

                            var profilesToUnRegister = oldData.Profiles.Except(profiles).ToArray();
                            UnSubscribeToProfileStates(identifier, profilesToUnRegister);

                            break;

                        default:

                            _logger.LogError($"Unknown event '{propertyEvent}' and action '{propertyAction}' from GoXLR.");

                            break;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.ToString());
                }
            };
            
            //socket.OnBinary = ...
            //socket.OnPing = (bytes) => _logger.LogInformation("Ping: {0}", Convert.ToBase64String(bytes));
            //socket.OnPong = (bytes) => _logger.LogInformation("Pong: {0}", Convert.ToBase64String(bytes));
            socket.OnError = (exception) => _logger.LogError(exception.ToString());
        }

        private void ConnectedAndSubscribeToRoutingStates(ClientIdentifier clientIdentifier)
        {
            //Register subscription to all possible routing as this is already known.
            var payload = RoutingStates
                .Select(context => new { action = "com.tchelicon.goxlr.routingtable", context = context.Key })
                .ToList();
            
            var json = JsonSerializer.Serialize(new
            {
                @event = "goxlrConnectionEvent",
                payload
            });

            Send(clientIdentifier, json);
        }

        private void SubscribeToProfileStates(ClientIdentifier clientIdentifier, string[] profiles)
        {
            foreach (var profile in profiles)
            {
                //It's required to send a propertyInspectorDidAppear at least once, not sure why.

                var propertyInspectorDidAppear = JsonSerializer.Serialize(new
                {
                    action = "com.tchelicon.goxlr.profilechange",
                    context = profile,
                    @event = "propertyInspectorDidAppear"
                });
                var willAppear = JsonSerializer.Serialize(new
                {
                    action = "com.tchelicon.goxlr.profilechange",
                    context = profile,
                    @event = "willAppear"
                });
                
                Send(clientIdentifier, propertyInspectorDidAppear);
                Send(clientIdentifier, willAppear);
            }
        }

        private void UnSubscribeToProfileStates(ClientIdentifier clientIdentifier, string[] profiles)
        {
            foreach (var profile in profiles)
            {
                var json = JsonSerializer.Serialize(new
                {
                    action = "com.tchelicon.goxlr.profilechange",
                    context = profile,
                    @event = "willDisappear"
                });

                Send(clientIdentifier, json);
            }
        }

        private void HandleProfileChangeSettingsEvent(ClientIdentifier clientIdentifier, string context)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context,
                @event = "didReceiveSettings",
                payload = new { settings = new { SelectedProfile = context } }
            });

            Send(clientIdentifier, json);
        }

        private void HandleRoutingTableSettingsEvent(ClientIdentifier clientIdentifier, string context)
        {
            var segments = context.Split('|');
            var routingInput = segments[0];
            var routingOutput = segments[1];
            
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.routingtable",
                context,
                @event = "didReceiveSettings",
                payload = new
                {
                    settings = new
                    {
                        RoutingAction = "Toggle",
                        RoutingInput = routingInput,
                        RoutingOutput = routingOutput
                    }
                }
            });
            
            Send(clientIdentifier, json);
        }
        
        /// <summary>
        /// Fetching profiles from the selected GoXLR App.
        /// </summary>
        /// <param name="clientIdentifier"></param>
        private void FetchProfiles(ClientIdentifier clientIdentifier)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context = "fetchingProfiles",
                @event = "propertyInspectorDidAppear"
            });
            
            Send(clientIdentifier, json);
        }
        
        /// <summary>
        /// Sets a profile in the selected GoXLR App.
        /// </summary>
        /// <param name="clientIdentifier"></param>
        /// <param name="profileName"></param>
        public void SetProfile(ClientIdentifier clientIdentifier, string profileName)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                @event = "keyUp",
                payload = new
                {
                    settings = new
                    {
                        SelectedProfile = profileName
                    }
                }
            });
            
            Send(clientIdentifier, json);
        }

        /// <summary>
        /// Sets a routing in the selected GoXLR App.
        /// </summary>
        /// <param name="clientIdentifier"></param>
        /// <param name="action"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public void SetRouting(ClientIdentifier clientIdentifier, string action, string input, string output)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.routingtable",
                @event = "keyUp",
                payload = new
                {
                    settings = new
                    {
                        RoutingAction = action,
                        RoutingInput = input,
                        RoutingOutput = output
                    }
                }
            });
            
            Send(clientIdentifier, json);
        }

        private void Send(ClientIdentifier clientIdentifier, string message)
        {
            if (clientIdentifier is null || !_sockets.TryGetValue(clientIdentifier, out var connection))
            {
                _logger.LogWarning($"No socket found on: {clientIdentifier}");
                return;
            }

            _logger.LogWarning("Send message: " + message);
            _ = connection.Send(message);
        }
    }
}
