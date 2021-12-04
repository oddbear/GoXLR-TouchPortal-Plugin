using System;
using System.Text.Json;
using System.Threading;
using Fleck;
using GoXLR.Server.Commands;
using GoXLR.Server.Configuration;
using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Handlers;
using GoXLR.Server.Models;
using Lamar;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoXLR.Server
{
    // ReSharper disable InconsistentNaming
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "This is they we write it.")]
    public class GoXLRServer
    {
        private readonly ILogger _logger;
        private readonly IContainer _container;
        private readonly WebSocketServerSettings _settings;
        
        public GoXLRState State { get; private set; }

        private IGoXLREventHandler _eventHandler;
        
        public const char RoutingSeparator = '|'; //TODO: Create better logic?

        public GoXLRServer(ILogger<GoXLRServer> logger,
            IOptions<WebSocketServerSettings> options,
            IContainer container)
        {
            _logger = logger;
            _container = container;
            _settings = options.Value;

            //var profileFetcherThread = new Thread(FetchProfilesThreadSync) { IsBackground = true };
            //profileFetcherThread.Start();
        }
        
        /// <summary>
        /// Starts the WebSockets server.
        /// </summary>
        public void Start()
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
                    //Get updated profiles:
                    State?.CommandHandler?.Send(new RequestProfilesCommand());
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
            var nestedContainerScope = _container.GetNestedContainer();
            nestedContainerScope.Inject(socket);

            //Add ctor for state, socket should be here maybe?
            var state = nestedContainerScope.GetInstance<GoXLRState>();
            State = state; //todo: Remove (need to find a better approach).

            //var plugin = ActivatorUtilities.CreateInstance<GoXLRPlugin>(provider);

            //TODO: How to get this from scope, on the ones using it by the Plugin (something needs to be removed from this class)?:
            _eventHandler = state.EventHandler;

            var notifier = nestedContainerScope.GetInstance<Notifier>();

            _eventHandler?.ConnectedClientChangedEvent(state.Client);

            socket.OnOpen = () =>
            {
                _logger.LogInformation($"Connection opened {socket.ConnectionInfo.ClientIpAddress}.");
            };

            socket.OnClose = () =>
            {
                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");

                //UpdateProfilesEvent?.Invoke(_profiles); //This is a list and  not a state, should we clean this up?
                state.EventHandler?.ConnectedClientChangedEvent(ConnectedClient.Empty);
                state.EventHandler?.ProfileSelectedChangedEvent(Profile.Empty);

                //Remove tenant:
                state.Profiles = Array.Empty<Profile>();
                nestedContainerScope.Dispose();
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
                    var propertyPayload = root.GetPayload();

                    var notification = new MessageNotification
                    {
                        Action = propertyAction,
                        Event = propertyEvent,
                        Context = propertyContext,
                        Payload = propertyPayload
                    };

                    _logger.LogInformation($"Handling: {propertyEvent}");
                    notifier.Publish(notification);

                    //TODO: Remove, but need to have a handled or similar state...
                    switch (propertyEvent)
                    {
                        case "goxlrConnectionEvent":
                            break;

                        case "getSettings"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":
                            break;

                        case "getSettings"
                            when propertyAction == "com.tchelicon.goxlr.routingtable":
                            break;

                        case "setState"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":
                            break;

                        case "setState"
                            when propertyAction == "com.tchelicon.goxlr.routingtable":
                            break;

                        case "sendToPropertyInspector"
                            when propertyAction == "com.tchelicon.goxlr.profilechange":
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
        /// Sets a profile in the selected GoXLR App.
        /// </summary>
        /// <param name="profile"></param>
        public void SetProfile(Profile profile)
        {
            State?.CommandHandler.Send(new SetProfileCommand(profile)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets a routing in the selected GoXLR App.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="routing"></param>
        public void SetRouting(RoutingAction action, Routing routing)
        {
            State?.CommandHandler.Send(new SetRoutingCommand(action, routing)).GetAwaiter().GetResult();
        }
    }

    public class GoXLRState
    {
        private readonly IWebSocketConnection _socket;

        public Profile[] Profiles { get; set; } = Array.Empty<Profile>();

        public ConnectedClient Client { get; }

        public IGoXLREventHandler EventHandler { get; }
        public CommandHandler CommandHandler { get; }

        public GoXLRState(
            IWebSocketConnection socket,
            IGoXLREventHandler eventHandler,
            ILogger<CommandHandler> logger)
        {
            _socket = socket;
            CommandHandler = new CommandHandler(socket, logger);
            EventHandler = eventHandler;

            var client = socket.ConnectionInfo.ClientIpAddress;
            Client = new ConnectedClient(client);
        }
    }
}
