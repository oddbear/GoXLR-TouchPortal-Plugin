using System;
using Fleck;
using GoXLR.Server.Models;

namespace GoXLR.Server
{
    public class GoXLRState
    {
        public Profile[] Profiles { get; set; } = Array.Empty<Profile>();
    }
}
