using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        public GoXLRServer(ILogger<GoXLRServer> logger,
            IOptions<WebSocketServerSettings> options)
        {
            _logger = logger;
            _settings = options.Value;
        }

        /// <summary>
        /// Starts the WebSockets server.
        /// </summary>
        public void Init()
        {
            var server = new WebSocketServer($"ws://{_settings.IpAddress}:{_settings.Port}/?GOXLRApp");
            server.Start(OnClientConnecting);
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
                
                //Get updated profiles:
                FetchProfiles(identifier);
            };

            socket.OnClose = () =>
            {
                _sockets.Remove(identifier);
                ClientData.RemoveAll(clientData => clientData.ClientIdentifier == identifier);

                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");

                UpdateConnectedClientsEvent?.Invoke();
            };

            socket.OnBinary = (bytes) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(bytes);
                    var action = socket.OnMessage;
                    if (action == null)
                    {
                        _logger.LogWarning("Binary: OnMessage not registered.");
                    }
                    else
                    {
                        action(message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
            };

            socket.OnMessage = (message) =>
            {
                _logger.LogInformation("Message: {0}", message);

                try
                {
                    var document = JsonSerializer.Deserialize<JsonDocument>(message);
                    if (document is null)
                        return;

                    var root = document.RootElement;
                    var propertyAction = root.GetValue<string>("action");
                    var propertyEvent = root.GetValue<string>("event");

                    var changeProfileActions = new[]
                    {
                        "com.tchelicon.goxlr.profilechange", //SD plugin v0.17+
                        "com.tchelicon.goXLR.ChangeProfile" //obsolete pre-0.17
                    };

                    if (changeProfileActions.Contains(propertyAction) &&
                        propertyEvent == "sendToPropertyInspector")
                    {
                        //Format:
                        var profiles = root
                            .GetProperty("payload")
                            .GetProperty("Profiles")
                            .EnumerateArray()
                            .Select(element => element.GetString())
                            .ToArray();

                        //Set client data:
                        var clientData = new ClientData(identifier, profiles);
                        ClientData.Add(clientData);

                        //Update client list, with data:
                        UpdateConnectedClientsEvent?.Invoke();
                    }
                    else
                    {
                        _logger.LogWarning($"Unknown event '{propertyEvent}' and action '{propertyAction}' from GoXLR.");
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.ToString());
                }
            };

            //socket.OnPing = (bytes) => _logger.LogInformation("Ping: {0}", Convert.ToBase64String(bytes));
            //socket.OnPong = (bytes) => _logger.LogInformation("Pong: {0}", Convert.ToBase64String(bytes));
            socket.OnError = (exception) => _logger.LogError(exception.ToString());
        }

        /// <summary>
        /// Fetching profiles from the selected GoXLR App.
        /// </summary>
        /// <param name="clientIdentifier"></param>
        private void FetchProfiles(ClientIdentifier clientIdentifier)
        {
            if (clientIdentifier is null || !_sockets.TryGetValue(clientIdentifier, out var connection))
            {
                _logger.LogWarning($"No socket found on: {clientIdentifier}");
                return;
            }
            
            //Sanitize:
            var contextId = JsonSerializer.Serialize($"{clientIdentifier.ClientIpAddress}:{clientIdentifier.ClientPort}");

            //Build:
            var json = $"{{\"action\":\"com.tchelicon.goxlr.profilechange\",\"context\":{contextId},\"event\":\"propertyInspectorDidAppear\"}}";

            //Send:
            _ = connection.Send(json);
        }
        
        /// <summary>
        /// Sets a profile in the selected GoXLR App.
        /// </summary>
        /// <param name="clientIdentifier"></param>
        /// <param name="profileName"></param>
        public void SetProfile(ClientIdentifier clientIdentifier, string profileName)
        {
            if (clientIdentifier is null || !_sockets.TryGetValue(clientIdentifier, out var connection))
            {
                _logger.LogWarning($"No socket found on: {clientIdentifier}");
                return;
            }

            //Sanitize:
            profileName = JsonSerializer.Serialize(profileName);

            //Build:
            var json = $"{{\"action\":\"com.tchelicon.goxlr.profilechange\",\"event\":\"keyUp\",\"payload\":{{\"settings\":{{\"SelectedProfile\":{profileName}}}}}}}";

            //Send:
            _ = connection.Send(json);
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
            if (clientIdentifier is null || !_sockets.TryGetValue(clientIdentifier, out var connection))
            {
                _logger.LogWarning($"No socket found on: {clientIdentifier}");
                return;
            }

            //Sanitize:
            action = JsonSerializer.Serialize(action);
            input = JsonSerializer.Serialize(input);
            output = JsonSerializer.Serialize(output);

            //Build:
            var json = $"{{\"action\":\"com.tchelicon.goxlr.routingtable\",\"event\":\"keyUp\",\"payload\":{{\"settings\":{{\"RoutingAction\":{action},\"RoutingInput\":{input},\"RoutingOutput\":{output}}}}}}}";

            //Send:
            _ = connection.Send(json);
        }
    }
}
