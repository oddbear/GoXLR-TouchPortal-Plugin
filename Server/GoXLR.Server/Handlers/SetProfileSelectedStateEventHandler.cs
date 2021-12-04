using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class SetProfileSelectedStateEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly ILogger<SetProfileSelectedStateEventHandler> _logger;

        public SetProfileSelectedStateEventHandler(
            GoXLRState state,
            ILogger<SetProfileSelectedStateEventHandler> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            if (message.Event != "setState")
                return;

            if (message.Action != "com.tchelicon.goxlr.profilechange")
                return;

            _logger.LogInformation("SetProfileSelectedStateEventHandler");

            var profileState = message.Payload.GetStateFromPayload();

            if (profileState != State.On)
                return;

            _state.EventHandler.ProfileSelectedChangedEvent(new Profile(message.Context));
        }
    }
}
