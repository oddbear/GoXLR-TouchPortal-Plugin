using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Commands;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class ProfileChangeSettingsEventHandler : INotificationHandler
    {
        private readonly CommandHandler _commandHandler;

        public ProfileChangeSettingsEventHandler(CommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        [Event("getSettings"), Action("com.tchelicon.goxlr.profilechange")]
        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            if (message.Context == "fetchingProfiles")
                return;

            //Part of the registration chain when registering a state subscription event:
            await _commandHandler.Send(new RespondCanReceiveProfileStateCommand(message.Context), cancellationToken);
        }
    }
}
