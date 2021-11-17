using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Fleck;
using GoXLR.Server.Commands;
using GoXLR.Server.Configuration;
using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
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
        
        private CommandHandler _commandHandler;

        private Profile[] _profiles;

        private IGoXLREventHandler _eventHandler;
        
        public const char RoutingSeparator = '|'; //TODO: Create better logic?

        public GoXLRServer(ILogger<GoXLRServer> logger,
            IOptions<WebSocketServerSettings> options)
        {
            _logger = logger;
            _settings = options.Value;
            
            //var profileFetcherThread = new Thread(FetchProfilesThreadSync) { IsBackground = true };
            //profileFetcherThread.Start();
        }
        
        /// <summary>
        /// Starts the WebSockets server.
        /// </summary>
        public void Init()
        {
            var server = new WebSocketServer($"ws://{_settings.IpAddress}:{_settings.Port}/?GOXLRApp");
            server.Start(OnClientConnecting);
        }

        public void SetEventHandler(IGoXLREventHandler eventHandler)
        {
            _eventHandler = eventHandler;
        }

        private void FetchProfilesThreadSync()
        {
            while (true)
            {
                try
                {
                    if (_commandHandler?.IsAvailable != true)
                        continue;

                    //Get updated profiles:
                    RequestProfiles();
                }
                catch (Exception exception)
                {
                    _logger?.LogDebug(exception, "Something went wrong on the Listener Thread.");
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        /// <summary>
        /// Setting up Fleck WebSocket callbacks.
        /// </summary>
        /// <param name="socket"></param>
        private void OnClientConnecting(IWebSocketConnection socket)
        {
            _commandHandler = new CommandHandler(socket, _logger);

            _profiles = Array.Empty<Profile>();

            var client = socket.ConnectionInfo.ClientIpAddress;
            _eventHandler?.ConnectedClientChangedEvent(new ConnectedClient(client));

            socket.OnOpen = () =>
            {
                _logger.LogInformation($"Connection opened {socket.ConnectionInfo.ClientIpAddress}.");
            };

            socket.OnClose = () =>
            {
                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");

                _profiles = Array.Empty<Profile>();

                //UpdateProfilesEvent?.Invoke(_profiles); //This is a list and  not a state, should we clean this up?
                _eventHandler?.ConnectedClientChangedEvent(ConnectedClient.Empty);
                _eventHandler?.ProfileSelectedChangedEvent(Profile.Empty);
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

                    var propertyAction = root.GetAction();
                    var propertyEvent = root.GetEvent();
                    var propertyContext = root.GetContext();

                    _logger.LogInformation($"Handling: {propertyEvent}");
                    switch (propertyEvent)
                    {
                        case "goxlrConnectionEvent":

                            HandleConnectedEventAndSubscribeToRoutingStates();
                            RequestProfiles();

                            break;

                        case "getSettings"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":

                            //We don't care about this "button". This is "global", and there is no state to ask for.
                            if (propertyContext == "fetchingProfiles")
                                break;

                            HandleProfileChangeSettingsEvent(propertyContext);

                            break;

                        case "getSettings"
                            when propertyAction == "com.tchelicon.goxlr.routingtable":
                            
                            HandleRoutingTableSettingsEvent(propertyContext);

                            break;

                        case "setState"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":

                            var profileState = root.GetStateFromPayload();

                            if (profileState == State.On)
                                _eventHandler?.ProfileSelectedChangedEvent(new Profile(propertyContext));

                            break;

                        case "setState"
                            when propertyAction == "com.tchelicon.goxlr.routingtable":

                            var routingState = root.GetStateFromPayload();
                            
                            if (!Routing.TryParseContext(propertyContext, out var routing))
                                break;

                            _eventHandler?.RoutingStateChangedEvent(routing, routingState);

                            break;

                        case "sendToPropertyInspector"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":
                            
                            var profiles = root.GetProfilesFromPayload();
                            
                            var (added, removed) = _profiles.Diff(profiles);

                            if (!added.Any() && !removed.Any())
                                break;

                            //Profiles has changed:
                            _profiles = profiles;
                            _eventHandler?.ProfileListChangedEvent(profiles);

                            SubscribeToProfileStates(added);
                            UnSubscribeToProfileStates(removed);

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

        /// <summary>
        /// Fetching profiles from the selected GoXLR App.
        /// </summary>
        private void RequestProfiles()
        {
            _commandHandler.Send(new RequestProfilesCommand());
        }

        /// <summary>
        /// Sets a profile in the selected GoXLR App.
        /// </summary>
        /// <param name="profile"></param>
        public void SetProfile(Profile profile)
        {
            _commandHandler.Send(new SetProfileCommand(profile));
        }

        /// <summary>
        /// Sets a routing in the selected GoXLR App.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="routing"></param>
        public void SetRouting(RoutingAction action, Routing routing)
        {
            _commandHandler.Send(new SetRoutingCommand(action, routing));
        }

        private void SubscribeToProfileStates(IEnumerable<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                _commandHandler.Send(new SubscribeToProfileStateCommand(profile));
            }
        }

        private void UnSubscribeToProfileStates(IEnumerable<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                _commandHandler.Send(new UnSubscribeToProfileStateCommand(profile));
            }
        }

        private void HandleConnectedEventAndSubscribeToRoutingStates()
        {
            _commandHandler.Send(new SubscribeToRoutingStatesCommand());
        }

        private void HandleProfileChangeSettingsEvent(string profileName)
        {
            //Part of the registration chain when registering a state subscription event:
            _commandHandler.Send(new RespondCanReceiveProfileStateCommand(profileName));
        }

        private void HandleRoutingTableSettingsEvent(string propertyContext)
        {
            if (!Routing.TryParseContext(propertyContext, out var routing))
                return;

            //Part of the registration chain when registering a state subscription event:
            _commandHandler.Send(new RespondCanReceiveRoutingStateCommand(propertyContext, routing));
        }
    }
}
