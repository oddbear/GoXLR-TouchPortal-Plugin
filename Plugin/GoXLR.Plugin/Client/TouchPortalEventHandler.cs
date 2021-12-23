using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GoXLR.Server;
using GoXLR.TouchPortal.Plugin.Configuration;
using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Messages.Events;

namespace GoXLR.TouchPortal.Plugin.Client
{
    public abstract class TouchPortalEventHandler : ITouchPortalEventHandler
    {
        public string PluginId => Identifiers.Id;

        private readonly GoXLRServer _server;
        private readonly ILogger<GoXLRPlugin> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public CancellationToken Token => _cancellationTokenSource.Token;

        protected TouchPortalEventHandler(
            GoXLRServer server,
            ILogger<GoXLRPlugin> logger)
        {
            _server = server;
            _logger = logger;
        }

        public abstract Task OnActionEvent(ActionEvent message);

        void ITouchPortalEventHandler.OnActionEvent(ActionEvent message)
        {
            Task.Run(() => OnActionEvent(message))
                .GetAwaiter()
                .GetResult();
        }

        void ITouchPortalEventHandler.OnInfoEvent(InfoEvent message)
        {
            _logger.LogInformation("Connect Event: Plugin Connected to TouchPortal.");
        }

        void ITouchPortalEventHandler.OnClosedEvent(string message)
        {
            _cancellationTokenSource.Cancel();
            _server.Dispose();

            _logger.LogInformation("Close Event: Plugin Disconnected from TouchPortal.");
            Environment.Exit(0);
        }
        
        void ITouchPortalEventHandler.OnBroadcastEvent(BroadcastEvent message)
            => LogNotImplemented();

        void ITouchPortalEventHandler.OnConnecterChangeEvent(ConnectorChangeEvent message)
            => LogNotImplemented();

        void ITouchPortalEventHandler.OnListChangedEvent(ListChangeEvent message)
            => LogNotImplemented();

        void ITouchPortalEventHandler.OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message)
            => LogNotImplemented();

        void ITouchPortalEventHandler.OnSettingsEvent(SettingsEvent message)
            => LogNotImplemented();

        void ITouchPortalEventHandler.OnUnhandledEvent(string jsonMessage)
            => LogNotImplemented();

        private void LogNotImplemented([CallerMemberName] string callerName = "")
        {
            _logger.LogDebug($"{callerName}: Method not implemented, message ignored.");
        }
    }
}
