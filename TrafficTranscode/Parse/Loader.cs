using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TrafficTranscode.RealNet;

namespace TrafficTranscode
{
	public class RawFile
	{
		public string Contents { get; set; }
		public string Path { get; set; }
	}

	public class Channel
	{
		public int Id { get; set; }
		public int Input { get; set; }
		public string UId { get; set; }
	}

	public class DataFile
	{
		public string City { get; set; }
		public Intersection Intersection { get; set; }
		public DateTime Start { get; set; }
		public DateTime Finish { get; set; }
		public IEnumerable<string> DataChannels { get; set; }

	}

    public class Loader
    {
        public List<RawFile> RawFiles { get; set; }
		public Parser Parser { get; set; }

        public Loader()
        {
			RawFiles = new List<RawFile>();
        }

        public Loader(string path) : this()
        {
            Load(path);
        }

        public void Load (string path)
		{
			if (Directory.Exists (path))
			{
				foreach (var file in Directory.GetFiles(path).Union(Directory.GetDirectories(path)))
				{
					Load (file);
				}
			}

			if (File.Exists (path))
			{
				var info = new FileInfo(path);
				if (info.Extension != "reg" && info.Extension != "log")
				{
					File.AppendAllText("unknown_extensions.txt", info.Extension + "\n");
					return;
				}

				RawFiles.Add(new RawFile() 
				{
					Path = path,
					Contents = File.ReadAllText(path)
				});
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

