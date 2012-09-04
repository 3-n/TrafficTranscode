using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TrafficTranscode.Parse;

namespace TrafficTranscode.MetaNet
{
    public class DataFile
    {
        public string City { get; set; }
        public MetaIntersection Node { get; set; }
        public TimeSpan TimeResolution { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public IEnumerable<Channel> DataChannels { get; set; }
        public int DeclaredChannelCount { get; set; }
        public int DeclaredRecordCount { get; set; }
        public IEnumerable<string> RecordLines { get; set; }

        private RawFile raw;

        public IEnumerable<Record> Records 
        { 
            get
            {
                var uIds = DataChannels.Select(ch => ch.UId);
                var headers = raw.LineStarting("Rekord").Words().Skip(3).Take(DeclaredChannelCount).ToList();

                var returnedRecords = new List<Record>();

                foreach (var recordLine in RecordLines)
                {
                    var words = recordLine.Words();
                    var recordId = words[0];
                    var rawDate = words[1];
                    var rawTime = words[2];
                    var chHeaders = headers.ToList();
                    var rawIntensities = words.Skip(3).Take(DeclaredChannelCount).ToList();

                    int throwaway;
                    if (!Int32.TryParse(words.Last(), out throwaway))
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
                                                                        Traffic = ibc.Value,
                                                                        City = City,
                                                                        Channel =
                                                                            DataChannels.Single(
                                                                                dch => dch.UId == ibc.Key),
                                                                        Duriation = TimeResolution,
                                                                        Node = Node,
                                                                        Start =
                                                                            ParseHelp.DateTimeParse(rawDate, rawTime),
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
                                        UId = line.Words().Last()
                                    });
            Node = new MetaIntersection
                       {
                           Name = Regex.Match(rawFile.Contents, @"(?<=Skrzyżowanie\ +:\ +)[\p{L}\p{Pd}]").Value,
                           Channels = DataChannels,
                           Intersections = rawFile.GuessIntersections()
                
                       };

            TimeResolution = rawFile.TimeResolution();

            Start = ParseHelp.DateTimeParse(rawFile
                                                .LineStarting(("Data sta"))
                                                .Words()
                                                .Last(),
                                            rawFile
                                                .LineStarting("Czas sta")
                                                .Words()
                                                .Last());
            Finish = ParseHelp.DateTimeParse(rawFile
                                                .LineStarting(("Data ko"))
                                                .Words()
                                                .Last(),
                                            rawFile
                                                .LineStarting("Czas ko")
                                                .Words()
                                                .Last());

            DeclaredRecordCount = Int32.Parse(rawFile.LineStarting("Liczba rekordów odczytanych do PC")
                                                  .Split(ParseHelp.WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                                                  .Last());

            DeclaredChannelCount = Int32.Parse(rawFile.LineStarting("Ilość").Words().Last());

            RecordLines = rawFile.Lines().SkipWhile(line => !line.StartsWith("Rekord:")).Skip(1);

        }
    }
}