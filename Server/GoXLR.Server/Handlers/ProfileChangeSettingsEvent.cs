using GoXLR.Server.Commands;

namespace GoXLR.Server.Handlers
{
    internal class ProfileChangeSettingsEvent
    {
        public static void Handle(CommandHandler commandHandler, string propertyContext)
        {
            //We don't care about this "button". This is "global", and there is no state to ask for.
            if (propertyContext == "fetchingProfiles")
                return;

            //Part of the registration chain when registering a state subscription event:
            commandHandler.Send(new RespondCanReceiveProfileStateCommand(propertyContext));
        }
    }
}
