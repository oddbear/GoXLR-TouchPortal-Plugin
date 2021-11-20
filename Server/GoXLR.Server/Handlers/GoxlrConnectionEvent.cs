using GoXLR.Server.Commands;

namespace GoXLR.Server.Handlers
{
    internal static class GoxlrConnectionEvent
    {
        public static void Handle(CommandHandler commandHandler)
        {
            commandHandler.Send(new RequestProfilesCommand());
            commandHandler.Send(new SubscribeToRoutingStatesCommand());
        }
    }
}
