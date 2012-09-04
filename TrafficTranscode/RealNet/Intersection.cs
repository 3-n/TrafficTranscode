using System;
using System.Collections.Generic;
using TrafficTranscode.MetaNet;

namespace TrafficTranscode.RealNet
{
    public class Intersection
    {
        public string Id { get; set; }
        public IEnumerable<Channel> Channels { get; set; }
    }

}

