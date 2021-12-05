using GoXLR.Server.Handlers.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers.Interfaces
{
    public interface INotificationHandler
    {
        Task Handle(MessageNotification message, CancellationToken cancellationToken);
    }
}
