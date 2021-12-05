using GoXLR.Server.Enums;
using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using GoXLR.Server.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class SetProfileSelectedStateEventHandler : INotificationHandler
    {
        private readonly IGoXLREventHandler _eventHandler;

        public SetProfileSelectedStateEventHandler(IGoXLREventHandler eventHandler)
        {
            _eventHandler = eventHandler;
        }

        [Event("setState"), Action("com.tchelicon.goxlr.profilechange")]
        public Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            var profileState = message.GetStateFromPayload();

            if (profileState != State.On)
                return Task.CompletedTask;

            _eventHandler.ProfileSelectedChangedEvent(new Profile(message.Context));

            return Task.CompletedTask;
        }
    }
}
