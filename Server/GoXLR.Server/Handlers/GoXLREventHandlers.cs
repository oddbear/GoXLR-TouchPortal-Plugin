using GoXLR.Server.Commands;
using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class GoxlrConnectionEventHandler : INotificationHandler
    {
        private readonly CommandHandler _commandHandler;

        public GoxlrConnectionEventHandler(CommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        [Event("goxlrConnectionEvent")]
        public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
        {
            //First send the respons that we have gotten the connected event, and tell that we want the states for routing.
            //All the possible states combinations are known, and we can ask for them here:
            await _commandHandler.Send(new SubscribeToRoutingStatesCommand(), cancellationToken);

            //Then we need to request the list of profiles, so we can subscribe to profile changes.
            //The list of profiles are dynamic (user created), and unknown, therefore we need to ask for it:
            await _commandHandler.Send(new RequestProfilesCommand(), cancellationToken);
        }
    }
}
