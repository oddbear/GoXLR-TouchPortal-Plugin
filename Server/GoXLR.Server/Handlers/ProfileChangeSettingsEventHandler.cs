using GoXLR.Server.Commands;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class ProfileChangeSettingsEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly ILogger<ProfileChangeSettingsEventHandler> _logger;

        public ProfileChangeSettingsEventHandler(
            GoXLRState state,
            ILogger<ProfileChangeSettingsEventHandler> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            if (message.Event != "getSettings")
                return;

            if (message.Action != "com.tchelicon.goxlr.profilechange")
                return;

            _logger.LogInformation("ProfileChangeSettingsEventHandler");

            if (message.Context == "fetchingProfiles")
                return;

            //Part of the registration chain when registering a state subscription event:
            await _state.CommandHandler.Send(new RespondCanReceiveProfileStateCommand(message.Context));
        }
    }
}
