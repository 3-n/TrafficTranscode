using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TrafficTranscode.RealNet;
using TrafficTranscode.Parse;

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

    public class MetaIntersection
    {
        public string Name { get; set; }
        public IEnumerable<Intersection> Intersections { get; set; }
        public IEnumerable<Channel> Channels { get; set; }
    }

    public enum FileType
    {
        Unknown, 
        Registry, 
        Log, 
        Image
    }

    public class RawFile
    {
        public string Contents { get; set; }
        public string Path { get; set; }

        public FileType Type
        {
            get
            {
                switch (Path.Substring(Path.Length-3, 3))
                {
                    case "rej":
                        return FileType.Registry;
                    case "log":
                        return FileType.Registry;
                    default:
                        return FileType.Unknown;
                }
            }
        }
    }

    public class Channel
    {
        public int Id { get; set; }
        public int Input { get; set; }
        public string UId { get; set; }

        public override string ToString()
        {
            return UId;
        }
    }

    public class DataFile
    {
        public string City { get; set; }
        public MetaIntersection Node { get; set; }
        public TimeSpan TimeResolution { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public IEnumerable<Channel> DataChannels { get; set; }
        public int DeclaredRecordCount { get; set; }
        public IEnumerable<string> RecordLines { get; set; }

        private RawFile raw;

        public IEnumerable<Record> Records 
        { 
            get
            {
                var uIds = DataChannels.Select(ch => ch.UId);
                var headers = raw.LineStarting("Rekord:").Words().Skip(3).ToList();

                var returnedRecords = new List<Record>();

                foreach (var recordLine in RecordLines)
                {
                    var words = recordLine.Words();
                    var recordId = words[0];
                    var rawDate = words[1];
                    var rawTime = words[2];
                    var chHeaders = headers.Skip(3).ToList();
                    var rawIntensities = words.Skip(3).ToList();

                    int throwaway;
                    if (!Int32.TryParse(rawIntensities.Last(), out throwaway))
                    {
                        Console.WriteLine("File {0} contains measurement-produced error.");
                        continue;
                    }

                    var intensityByChannel = chHeaders
                        .Zip(rawIntensities, (ch, i) => new KeyValuePair<string, int>(ch, Int32.Parse(i)))
                        .ToDictionary(p => p.Key, p => p.Value);

                    returnedRecords.AddRange(intensityByChannel
                        .Select(ibc => new Record
                        {
                            City = City,
                            Channel =
                                DataChannels.Single(
                                    dch => dch.UId == ibc.Key),
                            Duriation = TimeResolution,
                            Node = Node,
                            Start =
                                DateTime.Parse(String.Format("{0}T{1}",
                                                                rawDate,
                                                                rawTime)),
                            Error = false,
                            ErrorMessage = "no error",
                            Intersection = null, //TODO: hint: hard
                            Status = 0 //TODO: this is here because, again?
                        }));
                }

                return returnedRecords;
            }
        } 


        public DataFile(RawFile rawFile)
        {
            switch (rawFile.Type)
            {
                case FileType.Registry:
                    InitFromRegistry(rawFile);
                    break;
                default:
                    throw new NotSupportedException(String.Format("Filetype denoted by path {0} not supported", rawFile.Path));
            }

        }

        private void InitFromRegistry(RawFile rawFile)
        {
            raw = rawFile;
            //var lines = rawFile.Contents.Split(ParseHelp.LineSeparators, StringSplitOptions.RemoveEmptyEntries);
            City = Regex.Match(rawFile.Contents, @"(?<=Miasto\ +:\ +)\p{L}").Value;
            DataChannels = rawFile
                .Lines(line => line.StartsWith("Lp."))
                .Select(line => new Channel
                                    {
                                        Id = Int32.Parse(Regex.Matches(line, @"[0-9]+")[0].Value),
                                        Input = Int32.Parse(Regex.Matches(line, @"[0-9]+")[1].Value),
                                        UId = line.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries).Last()
                                    });
            Node = new MetaIntersection
            {
                Name = Regex.Match(rawFile.Contents, @"(?<=Miasto\ +:\ +)[\p{L}\p{Pd}]").Value,
                Channels = DataChannels,
                Intersections = rawFile.GuessIntersections()
                
            };

            TimeResolution = rawFile.TimeResolution();

            Start = DateTime.Parse(String.Format("{0}T{1}",
                rawFile.LineStarting("Data sta")
                    .Split(ParseHelp.WordSeparators,
                        StringSplitOptions.RemoveEmptyEntries)
                    .Last(),
                rawFile.LineStarting("Czas sta"))
                    .Split(ParseHelp.WordSeparators,
                        StringSplitOptions.RemoveEmptyEntries)
                    .Last());
            Finish = DateTime.Parse(String.Format("{0}T{1}",
                rawFile.LineStarting(("Data ko"))
                    .Split(ParseHelp.WordSeparators,
                        StringSplitOptions.RemoveEmptyEntries)
                    .Last(),
                rawFile.LineStarting("Czas ko"))
                    .Split(ParseHelp.WordSeparators,
                        StringSplitOptions.RemoveEmptyEntries)
                    .Last());

            DeclaredRecordCount = Int32.Parse(rawFile.LineStarting("Liczba rekordów odczytanych do PC")
                .Split(ParseHelp.WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Last());
            RecordLines = rawFile.Lines();

        }
    }
}

