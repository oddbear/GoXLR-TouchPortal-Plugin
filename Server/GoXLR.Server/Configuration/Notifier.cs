using GoXLR.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Configuration
{
    //TODO: Notifies and stuff...
    public class Notifier
    {
        private readonly IEnumerable<INotificationHandler> _notifies;

        public Notifier(IEnumerable<INotificationHandler> notifies)
        {
            _notifies = notifies;
        }

        public void Publish(MessageNotification message)
        {
            foreach (var notify in _notifies)
            {
                //TODO: Implement correctly:
                notify.Handle(message, CancellationToken.None).GetAwaiter().GetResult();
            }
            //TODO: If unhandled... report... decorator?
        }
    }
}
