using System.Collections.Generic;
using TrafficTranscode.RealNet;

namespace TrafficTranscode.MetaNet
{
    public class MetaIntersection
    {
        public string Name { get; set; }
        public IEnumerable<Intersection> Intersections { get; set; }
        public IEnumerable<Channel> Channels { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}