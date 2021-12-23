using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using GoXLR.Server.Configuration;
using GoXLR.Server.Enums;
using GoXLR.Server.Handlers.Commands;
using GoXLR.Server.Handlers.Models;
using GoXLR.Server.Models;
using Lamar;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoXLR.Server
{
    public sealed class GoXLRServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IContainer _container;
        private readonly WebSocketServerSettings _settings;

        private bool _started;
        private WebSocketServer _webSocketServer;
        private INestedContainer _lastConnection;

        public const char RoutingSeparator = '|'; //TODO: Create better logic?

        public GoXLRServer(
            ILogger<GoXLRServer> logger,
            IOptions<WebSocketServerSettings> options,
            IContainer container)
        {
            _logger = logger;
            _container = container;
            _settings = options.Value;
        }
        
        /// <summary>
        /// Starts the WebSockets server.
        /// </summary>
        public void Start()
        {
            if (_started)
                return;

            _started = true;

            _webSocketServer = new WebSocketServer($"ws://{_settings.IpAddress}:{_settings.Port}/?GOXLRApp");
            _webSocketServer.Start(OnClientConnecting);

            var profileFetcherThread = new Thread(FetchProfilesThreadSync) { IsBackground = true };
            profileFetcherThread.Start();
        }

        private void FetchProfilesThreadSync()
        {
            while (true)
            {
                try
                {
                    var commandHandler = _lastConnection?.GetInstance<CommandHandler>();

                    //Get updated profiles:
                    commandHandler?.Send(new RequestProfilesCommand(), CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Something went wrong on the Listener Thread.");
                }

                var sleepTime = TimeSpan.FromSeconds(10);
                Thread.Sleep(sleepTime);
            }
        }

        /// <summary>
        /// Setting up Fleck WebSocket callbacks.
        /// </summary>
        /// <param name="socket"></param>
        private void OnClientConnecting(IWebSocketConnection socket)
        {
            var eventHandler = _container.GetInstance<IGoXLREventHandler>();

            //This is a new connected device, so we inject the socket and creates a scoped state around this client.
            //If we want support for multiple clients, we can create a tracker (key:client,value:containerScope) of theese containers.
            var nestedContainerScope = _container.GetNestedContainer();
            nestedContainerScope.Inject(socket);

            //Set a ready container to be the last connected client:
            _lastConnection = nestedContainerScope;

            var notificationHandlerRouter = nestedContainerScope.GetInstance<NotificationHandlerRouter>();

            var client = socket.ConnectionInfo.ClientIpAddress;
            eventHandler.ConnectedClientChangedEvent(new ConnectedClient(client));

            socket.OnOpen = () => _logger.LogInformation($"Connection opened {socket.ConnectionInfo.ClientIpAddress}.");

            socket.OnMessage = message =>
            {
                try
                {
                    _logger.LogInformation($"Received message: {message}");

                    if (string.IsNullOrWhiteSpace(message))
                        return;

                    var jsonSerializerOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var notification = JsonSerializer.Deserialize<MessageNotification>(message, jsonSerializerOptions);
                    if (notification is null)
                        return;

                    var handeled = notificationHandlerRouter.RouteToHandler(notification, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();

                    if (!handeled)
                        _logger.LogInformation($"Unknown event '{notification.Event}' and action '{notification.Action}' from GoXLR.");

                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed on processing from message from the GoXLR App.");
                }
            };

            socket.OnClose = () =>
            {
                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");

                //Cleanup goxlr client scope:
                eventHandler.ConnectedClientChangedEvent(ConnectedClient.Empty);
                eventHandler.ProfileSelectedChangedEvent(Profile.Empty);

                _lastConnection = null;
                nestedContainerScope.Dispose();
            };

            socket.OnError = exception => _logger.LogError(exception, "WebSocket OnError.");
        }

        /// <summary>
        /// Sets a profile in the selected GoXLR App.
        /// </summary>
        /// <param name="profile"></param>
        public async Task SetProfile(Profile profile, CancellationToken cancellationToken)
        {
            var commandHandler = _lastConnection?.GetInstance<CommandHandler>();
            if (commandHandler is null)
                return;

            //Latest connected client wins:
            await commandHandler.Send(new SetProfileCommand(profile), cancellationToken);
        }

        /// <summary>
        /// Sets a routing in the selected GoXLR App.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="routing"></param>
        public async Task SetRouting(RoutingAction action, Routing routing, CancellationToken cancellationToken)
        {
            var commandHandler = _lastConnection?.GetInstance<CommandHandler>();
            if (commandHandler is null)
                return;

            //Latest connected client wins:
            await commandHandler.Send(new SetRoutingCommand(action, routing), cancellationToken);
        }

        public void Dispose()
        {
            _webSocketServer?.Dispose();
        }
    }
}
