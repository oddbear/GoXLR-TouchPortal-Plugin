using GoXLR.Models.Models.Shared;

namespace GoXLR.Models.Models
{
    public class GetProfilesRequest : ModelBase
    {
        public static GetProfilesRequest Create()
        {
            return new GetProfilesRequest
            {
                Action = "com.tchelicon.goxlr.profilechange",
                Context = "00000000000000000000000000000000",
                Event = "propertyInspectorDidAppear"
            };
        }
    }
}
