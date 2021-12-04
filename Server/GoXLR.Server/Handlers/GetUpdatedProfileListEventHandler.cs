using GoXLR.Server.Commands;
using GoXLR.Server.Extensions;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class GetUpdatedProfileListEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly ILogger<GetUpdatedProfileListEventHandler> _logger;

        public GetUpdatedProfileListEventHandler(
            GoXLRState state,
            ILogger<GetUpdatedProfileListEventHandler> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Event != "sendToPropertyInspector")
                return;

            if (notification.Action != "com.tchelicon.goxlr.profilechange")
                return;

            if (_state.CommandHandler is null)
                return;

            if (_state.EventHandler is null)
                return;

            _logger.LogInformation("GoxlrConnectionEventHandler");

            var profiles = notification.Payload.GetProfilesFromPayload();

            var (added, removed) = _state.Profiles.Diff(profiles);

            if (!added.Any() && !removed.Any())
                return;

            //Profiles has changed:
            _state.Profiles = profiles;
            _state.EventHandler.ProfileListChangedEvent(profiles);

            foreach (var profile in added)
            {
                await _state.CommandHandler.Send(new SubscribeToProfileStateCommand(profile));
            }

            foreach (var profile in removed)
            {
                await _state.CommandHandler.Send(new UnSubscribeToProfileStateCommand(profile));
            }
        }
    }
}
