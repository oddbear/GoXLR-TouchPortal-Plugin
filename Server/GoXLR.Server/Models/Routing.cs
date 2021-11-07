﻿using System;
using System.Collections.Generic;
using System.Linq;
using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;

namespace GoXLR.Server.Models
{
    public record Routing(RoutingInput Input, RoutingOutput Output)
    {
        public static bool TryParseDescription(string inputDescription, string outputDescription, out Routing routing)
        {
            try
            {
                if (!EnumExtensions.TryParseEnumFromDescription(inputDescription, out RoutingInput input))
                {
                    routing = default;
                    return false;
                }

                if (!EnumExtensions.TryParseEnumFromDescription(outputDescription, out RoutingOutput output))
                {
                    routing = default;
                    return false;
                }

                routing = new Routing(input, output);
                return true;
            }
            catch
            {
                routing = default;
                return false;
            }

        }
        
        public static bool TryParseContext(string context, out Routing routing)
        {
            try
            {
                var segments = context.Split(GoXLRServer.RoutingSeparator);
                var input = Enum.Parse<RoutingInput>(segments[0]);
                var output = Enum.Parse<RoutingOutput>(segments[1]);
                routing = new Routing(input, output);
                return true;
            }
            catch
            {
                routing = null;
                return false;
            }
        }

        public static IEnumerable<Routing> GetRoutingTable()
        {
            return
                from input in Enum.GetValues<RoutingInput>()
                from output in Enum.GetValues<RoutingOutput>()
                select new Routing(input, output) into routing

                where routing != new Routing(RoutingInput.Chat, RoutingOutput.ChatMic)
                where routing != new Routing(RoutingInput.Samples, RoutingOutput.Sampler)
                select routing;
        }
    }
}