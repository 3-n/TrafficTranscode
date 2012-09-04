using System;
using TrafficTranscode.RealNet;

namespace TrafficTranscode.MetaNet
{
    public class Record
    {
        public string City { get; set; }
        public MetaIntersection Node { get; set; }
        public DateTime Start { get; set; }
        public TimeSpan Duriation { get; set; }
        public Intersection Intersection { get; set; }
        public Channel Channel { get; set; }

        public int Status { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }

        public Record()
        {
        }
    }
}

