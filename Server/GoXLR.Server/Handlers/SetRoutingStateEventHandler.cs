using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using GoXLR.Server.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class SetRoutingStateEventHandler : INotificationHandler
    {
        private readonly IGoXLREventHandler _eventHandler;

        public SetRoutingStateEventHandler(IGoXLREventHandler eventHandler)
        {
            _eventHandler = eventHandler;
        }

        [Event("setState"), Action("com.tchelicon.goxlr.routingtable")]
        public Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            var routingState = message.GetStateFromPayload();

            if (!Routing.TryParseContext(message.Context, out var routing))
                return Task.CompletedTask;

            _eventHandler.RoutingStateChangedEvent(routing, routingState);

            return Task.CompletedTask;
        }
    }
}
