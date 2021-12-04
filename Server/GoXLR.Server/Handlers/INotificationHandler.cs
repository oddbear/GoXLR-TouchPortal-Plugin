using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public interface INotificationHandler
    {
        Task Handle(MessageNotification message, CancellationToken cancellationToken);
    }
}
