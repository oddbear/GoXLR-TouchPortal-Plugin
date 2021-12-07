using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Commands;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using GoXLR.Server.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class GetUpdatedProfileListEventHandler : INotificationHandler
    {
        private readonly CommandHandler _commandHandler;
        private readonly IGoXLREventHandler _eventHandler;

        private Profile[] _profiles = Array.Empty<Profile>();

        public GetUpdatedProfileListEventHandler(
            CommandHandler commandHandler,
            IGoXLREventHandler eventHandler)
        {
            _commandHandler = commandHandler;
            _eventHandler = eventHandler;
        }

        [Event("sendToPropertyInspector"), Action("com.tchelicon.goxlr.profilechange")]
        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            var profiles = message.GetProfilesFromPayload();

            var current = _profiles;

            var added = profiles.Except(current).ToArray();
            var removed = current.Except(profiles).ToArray();

            if (!added.Any() && !removed.Any())
                return;

            //Profiles has changed:
            _profiles = profiles;

            _eventHandler.ProfileListChangedEvent(profiles);

            foreach (var profile in added)
            {
                await _commandHandler.Send(new SubscribeToProfileStateCommand(profile), cancellationToken);
            }

            foreach (var profile in removed)
            {
                await _commandHandler.Send(new UnSubscribeToProfileStateCommand(profile), cancellationToken);
            }
        }
    }
}
