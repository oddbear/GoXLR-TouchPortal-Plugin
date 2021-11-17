using System.Collections.Generic;
using System.Linq;
using GoXLR.Server.Enums;
using GoXLR.Server.Models;
using GoXLR.TouchPortal.Plugin.Configuration;
using TouchPortalSDK.Interfaces;

namespace GoXLR.TouchPortal.Plugin.Client
{
    public class GoXLREventHandler : IGoXLREventHandler
    {
        private readonly ITouchPortalClient _client;

        //The issue with to many updates performance is towards TP, so this is where the filter should be.
        private readonly Dictionary<Routing, State> _stateTracker = new();

        public GoXLREventHandler(ITouchPortalClient client)
        {
            _client = client;
        }
        
        public void ConnectedClientChangedEvent(ConnectedClient client)
        {
            _client.StateUpdate(Identifiers.ConnectedClientId, client.Name);
        }
        
        public void ProfileListChangedEvent(Profile[] profiles)
        {
            var profileNames = profiles
                .Select(profile => profile.Name)
                .ToArray();

            _client.ChoiceUpdate(Identifiers.ProfileListId, profileNames);
        }

        public void ProfileSelectedChangedEvent(Profile profile)
        {
            if (profile is null)
                return;

            //TODO: Fix broken list, list is only updated the first time it's fetched (Issue from the GoXLR App 1.4.4.165):
            _client.StateUpdate(Identifiers.SelectedProfileId, profile.Name);
        }

        public void RoutingStateChangedEvent(Routing routing, State state)
        {
            if (routing is null)
                return;

            //TODO: Fix broken states, all in Samples column is broken now (Issue from the GoXLR App 1.4.4.165):
            if (_stateTracker.ContainsKey(routing) && _stateTracker[routing] == state)
                return;
            
            _stateTracker[routing] = state;

            var stateId = Identifiers.GetStateId(routing);
            _client.StateUpdate(stateId, state.ToString());
        }
    }
}
