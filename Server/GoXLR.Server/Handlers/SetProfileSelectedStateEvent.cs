using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using System.Text.Json;

namespace GoXLR.Server.Handlers
{
    internal static class SetProfileSelectedStateEvent
    {
        public static void Handle(IGoXLREventHandler eventHandler, JsonElement root)
        {
            if (eventHandler is null)
                return;

            var propertyContext = root.GetContext();
            var profileState = root.GetStateFromPayload();

            if (profileState == State.On)
                eventHandler.ProfileSelectedChangedEvent(new Profile(propertyContext));
        }
    }
}
