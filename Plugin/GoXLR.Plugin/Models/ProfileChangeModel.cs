using GoXLR.Server.Models;
using GoXLR.TouchPortal.Plugin.Configuration;
using TouchPortalSDK.Messages.Events;

namespace GoXLR.TouchPortal.Plugin.Models
{
    public class ProfileChangeModel
    {
        public Profile Profile { get; }

        private ProfileChangeModel(Profile profile)
        {
            Profile = profile;
        }
        
        public static bool TryParse(ActionEvent message, out ProfileChangeModel profileChangeModel)
        {
            profileChangeModel = default;

            try
            {
                if (message is null)
                    return false;

                var profileName = message[Identifiers.ProfileChangeRequestedId + ".data.profiles"];
                if (string.IsNullOrWhiteSpace(profileName))
                    return false;

                var profile = new Profile(profileName);
                profileChangeModel = new ProfileChangeModel(profile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}