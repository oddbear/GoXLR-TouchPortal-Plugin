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
        private readonly Dictionary<ClientIdentifier, ClientData> _clientData = new();

        public Action<ClientIdentifier[]> UpdateConnectedClientsEvent { get; set; }

        public GoXLRServer(ILogger<GoXLRServer> logger,
            IOptions<WebSocketServerSettings> options)
        {
            _logger = logger;

            _settings = options.Value;
        }

        public void Init()
        {
            var server = new WebSocketServer($"ws://{_settings.IpAddress}:{_settings.Port}/?GOXLRApp");
            server.Start(OnClientConnecting);
        }

        private void OnClientConnecting(IWebSocketConnection socket)
        {
            var connectionInfo = socket.ConnectionInfo;
            var identifier = new ClientIdentifier(connectionInfo.ClientIpAddress, connectionInfo.ClientPort);

            socket.OnOpen = () =>
            {
                _sockets.Add(identifier, socket);
                _logger.LogInformation($"Connection opened {socket.ConnectionInfo.ClientIpAddress}.");
                
                UpdateConnectedClientsEvent?.Invoke(_sockets.Keys.ToArray());

                //Get updated profiles:
                FetchProfiles(identifier);
            };

            socket.OnClose = () =>
            {
                _sockets.Remove(identifier);
                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");

                UpdateConnectedClientsEvent?.Invoke(_sockets.Keys.ToArray());
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
                    var propertyAction = root.GetProperty("action").GetString();
                    var propertyEvent = root.GetProperty("event").GetString();
                    
                    if (propertyAction == "com.tchelicon.goXLR.ChangeProfile" &&
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
                        _clientData[identifier] = clientData;
                    }
                    else
                    {
                        _logger.LogWarning("Unknown contextId from GoXLR.");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
            };

            //socket.OnPing = (bytes) => _logger.LogInformation("Ping: {0}", Convert.ToBase64String(bytes));
            //socket.OnPong = (bytes) => _logger.LogInformation("Pong: {0}", Convert.ToBase64String(bytes));
            socket.OnError = (exception) => _logger.LogError(exception.ToString());
        }

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

        public ClientData GetClientData(ClientIdentifier clientIdentifier)
        {
            if (clientIdentifier is null || !_clientData.TryGetValue(clientIdentifier, out var clientData))
            {
                _logger.LogWarning($"ClientData missing for: '{clientIdentifier}'");
                return null;
            }

            return clientData;

        }
        
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
