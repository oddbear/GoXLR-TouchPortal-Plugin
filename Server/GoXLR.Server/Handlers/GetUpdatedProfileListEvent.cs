using GoXLR.Server.Commands;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    internal static class GetUpdatedProfileListEvent
    {
        //TODO: Static:
        private static Profile[] _profiles;

        public static void Handle(CommandHandler commandHandler, IGoXLREventHandler eventHandler, JsonElement root)
        {
            if (commandHandler is null)
                return;

            if (eventHandler is null)
                return;

            var profiles = root.GetProfilesFromPayload();

            var (added, removed) = _profiles.Diff(profiles);

            if (!added.Any() && !removed.Any())
                return;

            //Profiles has changed:
            _profiles = profiles;
            eventHandler.ProfileListChangedEvent(profiles);

            foreach (var profile in added)
            {
                commandHandler.Send(new SubscribeToProfileStateCommand(profile));
            }

            foreach (var profile in removed)
            {
                commandHandler.Send(new UnSubscribeToProfileStateCommand(profile));
            }
        }
    }
}
