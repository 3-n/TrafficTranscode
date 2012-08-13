using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TrafficTranscode
{
    public class Loader
    {
        public IEnumerable<string> RawFiles { get; set; }

        public Loader()
        {
        }

        public Loader(string path)
        {
            Load(path);
        }

        public void Load(string path)
        {
            if(Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path).Union(Directory.GetDirectories(path)))
                {
                    Load(file);
                }
            }

        }
    }

    public enum FileType
    {
        Unknown, 
        Registry, 
        Log, 
        Image
    }

    public abstract class Parser
    {

    }

    public class RegistryParser : Parser
    {

    }

    public class LogParser : Parser
    {

    }
}

