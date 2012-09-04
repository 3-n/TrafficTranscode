using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrafficTranscode.MetaNet;

namespace TrafficTranscode.Parse
{


    public class Loader
    {
        public List<RawFile> RawFiles { get; set; }
        public List<DataFile> DataFiles { get; set; }
        public IEnumerable<Record> Records { get; private set; }

        private Loader()
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
				if (info.Extension != ".rej" && info.Extension != ".log")
				{
					File.AppendAllText("unknown_extensions.txt", info.Extension + "\n");
					return;
				}

				RawFiles.Add(new RawFile 
				{
					Path = path,
					Contents = File.ReadAllText(path, System.Text.Encoding.GetEncoding("windows-1250"))
				});
			}

            DataFiles = RawFiles.Select(rawFile => new DataFile(rawFile)).ToList();

            Records = DataFiles.SelectMany(df => df.Records);
        }
    }
}

