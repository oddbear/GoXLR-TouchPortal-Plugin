using GoXLR.Server.Extensions;
using GoXLR.Server.Models;

namespace GoXLR.TouchPortal.Plugin.Configuration
{
    public static class Identifiers
    {
        public const string Id = "oddbear.touchportal.goxlr";

        public const string ProfileListId = Id + ".profiles.action.change.data.profiles";
        
        public const string SelectedProfileId = Id + ".state.selectedProfile";
        
        public const string ConnectedClientId = Id + ".state.connectedClient";
        
        public const string RoutingTableChangeRequestedId = Id + ".routingtable.action.change";

        public const string ProfileChangeRequestedId = Id + ".profiles.action.change";

        public static string GetStateId(Routing routing)
        {
            var input = routing.Input.GetEnumDescription();
            var output = routing.Output.GetEnumDescription();
            var stateId = $"{Id}.state:({input}|{output})";

            return stateId;
        }
    }
}