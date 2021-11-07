using System.Collections.Generic;
using System.Linq;
using GoXLR.Server.Models;

namespace GoXLR.Server.Extensions
{
    public static class ProfileExtensions
    {
        public static (Profile[] Added, Profile[] Removed) Diff(this IEnumerable<Profile> current, IEnumerable<Profile> changed)
        {
            var added = changed.Except(current).ToArray();
            var removed = current.Except(changed).ToArray();

            return (added, removed);
        }
    }

}
