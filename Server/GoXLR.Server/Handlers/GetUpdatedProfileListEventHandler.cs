using GoXLR.Server.Commands;
using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using GoXLR.Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class GetUpdatedProfileListEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly CommandHandler _commandHandler;
        private readonly IGoXLREventHandler _eventHandler;

        public GetUpdatedProfileListEventHandler(
            GoXLRState state,
            CommandHandler commandHandler,
            IGoXLREventHandler eventHandler)
        {
            _state = state;
            _commandHandler = commandHandler;
            _eventHandler = eventHandler;
        }

        [Event("sendToPropertyInspector"), Action("com.tchelicon.goxlr.profilechange")]
        public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
        {
            var profiles = notification.GetProfilesFromPayload();

            var current = _state.Profiles;
            var (added, removed) = Diff(current, profiles);

            if (!added.Any() && !removed.Any())
                return;

            //Profiles has changed:
            _state.Profiles = profiles;
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

        private static (Profile[] Added, Profile[] Removed) Diff(IEnumerable<Profile> current, IEnumerable<Profile> changed)
        {
            var added = changed.Except(current).ToArray();
            var removed = current.Except(changed).ToArray();

            return (added, removed);
        }
    }
}
