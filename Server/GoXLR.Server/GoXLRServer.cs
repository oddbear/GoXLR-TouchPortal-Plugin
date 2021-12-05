using System;
using System.Text.Json;
using System.Threading;
using Fleck;
using GoXLR.Server.Commands;
using GoXLR.Server.Configuration;
using GoXLR.Server.Enums;
using GoXLR.Server.Handlers.Models;
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

        private bool _started;
        private CommandHandler _commandHandler;

        public const char RoutingSeparator = '|'; //TODO: Create better logic?

        public GoXLRServer(ILogger<GoXLRServer> logger,
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

            var server = new WebSocketServer($"ws://{_settings.IpAddress}:{_settings.Port}/?GOXLRApp");
            server.Start(OnClientConnecting);

            var profileFetcherThread = new Thread(FetchProfilesThreadSync) { IsBackground = true };
            profileFetcherThread.Start();
        }

        private void FetchProfilesThreadSync()
        {
            while (true)
            {
                try
                {
                    //Get updated profiles:
                    _commandHandler?.Send(new RequestProfilesCommand(), CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogDebug(exception, "Something went wrong on the Listener Thread.");
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
            //This is a new connected device, so we inject the socket and creates a scoped state around this client.
            //If we want support for multiple clients, we can create a tracker (key:client,value:containerScope) of theese containers.
            var nestedContainerScope = _container.GetNestedContainer();
            nestedContainerScope.Inject(socket);

            _commandHandler = nestedContainerScope.GetInstance<CommandHandler>();
            var eventHandler = nestedContainerScope.GetInstance<IGoXLREventHandler>();

            var notificationHandlerRouter = nestedContainerScope.GetInstance<NotificationHandlerRouter>();

            var client = socket.ConnectionInfo.ClientIpAddress;
            eventHandler.ConnectedClientChangedEvent(new ConnectedClient(client));

            socket.OnOpen = () => _logger.LogInformation($"Connection opened {socket.ConnectionInfo.ClientIpAddress}.");

            socket.OnMessage = message =>
            {
                try
                {
                    _logger.LogWarning("Received message: " + message);

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
                        _logger.LogError($"Unknown event '{notification.Event}' and action '{notification.Action}' from GoXLR.");

                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.ToString());
                }
            };

            socket.OnClose = () =>
            {
                _logger.LogInformation($"Connection closed {socket.ConnectionInfo.ClientIpAddress}.");

                //Cleanup goxlr client scope:
                eventHandler.ConnectedClientChangedEvent(ConnectedClient.Empty);
                eventHandler.ProfileSelectedChangedEvent(Profile.Empty);

                _commandHandler = null;
                nestedContainerScope.Dispose();
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
            //Latest connected client wins:
            _commandHandler?.Send(new SetProfileCommand(profile), CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Sets a routing in the selected GoXLR App.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="routing"></param>
        public void SetRouting(RoutingAction action, Routing routing)
        {
            //Latest connected client wins:
            _commandHandler?.Send(new SetRoutingCommand(action, routing), CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
    }
}
