using System;
using TrafficTranscode.RealNet;
using System.Collections.Generic;

namespace TrafficTranscode.MetaNet
{
    public class Record
    {
        public DateTime Start { get; set; }
        public TimeSpan Duriation { get; set; }
        public Intersection Intersection { get; set; }

        public int Status { get; set; }
        public int Error { get; set; }

        public Record()
        {
        }
    }

    public class Datafile
    {

    }

    public class MetaIntersection
    {
        public IEnumerable<Intersection> Intersections { get; set; }
    }
}

