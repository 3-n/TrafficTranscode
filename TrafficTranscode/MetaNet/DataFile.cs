using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private List<Record> returnedRecords;

        public IEnumerable<Record> Records 
        { 
            get
            {
                if(returnedRecords!=null)
                {
                    return returnedRecords;
                }

                

                returnedRecords = new List<Record>();

                switch (raw.Type)
                {
                    case FileType.Registry:
                        var uIds = DataChannels.Select(ch => ch.UId);
                        var headers = raw.LineStarting("Rekord").Words().Skip(3).Take(DeclaredChannelCount).ToList();

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
                                Console.WriteLine("File {0} contains measurement-produced error.", raw.Path);
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
                                                                                Duration = TimeResolution,
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

                    case FileType.Log:
                        var tables = raw.LogFileTableStrings().Select(str => str.LogFileTable());

                        foreach (var table in tables)
                        {
                            for (int i = 2; i < table.Length-1; i++)
                            {
                                for (int j = 1; j < table.First().Length; j++)
                                {
                                    var trafficRaw = Int32.Parse(table[i][j]);
                                    returnedRecords.Add(new Record
                                    {
                                        City = City,
                                        Channel = new Channel { UId = table[i][0] },
                                        Duration = TimeResolution,
                                        Error = trafficRaw >= 0,
                                        ErrorMessage = table[i][j],
                                        Intersection = null, //TODO: hint: hard
                                        Node = Node,
                                        Start = DateTime.Parse(String.Format("2012-{0}T{1}:00", table[0][j], table[1][j])),
                                        Status = trafficRaw,
                                        Traffic = Math.Max(0, trafficRaw)
                                    });
                                }
                            }
                        }

                        var maliciousDurations = returnedRecords
                            .Where( r => r.Duration.Ticks > TimeResolution.Ticks*5 || r.Duration.Ticks < TimeResolution.Ticks/5);

                        returnedRecords = returnedRecords
                            .Where(r =>
                                       {
                                           if(r.Duration.Ticks > TimeResolution.Ticks*5 || r.Duration.Ticks < TimeResolution.Ticks/5)
                                           {
                                               return false;
                                           }
                                           return true;
                                       })
                            .ToList();

                        File.WriteAllLines("malicious_durations.txt", maliciousDurations.Select(ts => ts.ToString()));

                        //var outOfResolutionRecords = returnedRecords.Where(r => r.Duration != TimeResolution);

                        var tooLong = returnedRecords.Where(r => r.Duration > TimeResolution);
                        var tooShort = returnedRecords.Where(r => r.Duration < TimeResolution);

                        var tooShortCorrected = tooShort.Stitch(TimeResolution) ?? new List<Record>();
                        var tooLongCorrected = tooLong.SelectMany(r => r.Split(TimeResolution));

                        return returnedRecords
                            .Except(tooLong)
                            .Except(tooShort)
                            .Union(tooShortCorrected)
                            .Union(tooLongCorrected);

                    default:
                        throw new FormatException(String.Format("Resolution of records not possible: {0}", raw.Path));
                }

            }
        } 


        public DataFile(RawFile rawFile)
        {
            switch (rawFile.Type)
            {
                case FileType.Registry:
                    InitFromRegistry(rawFile);
                    break;
                case FileType.Log:
                    InitFromLog(rawFile);
                    break;
                default:
                    throw new NotSupportedException(String.Format("Filetype denoted by path {0} not supported", rawFile.Path));
            }

        }

        private void InitFromLog(RawFile rawFile)
        {
            raw = rawFile;
            City = "Poznań"; // oh well
            DataChannels = rawFile
                .Lines(line => Regex.IsMatch(line, @"\ +\|\ [0-9A-Z]+\ \|"))
                .Select(line => line.Words().Skip(1).First())
                .Distinct()
                .Select(uid => new Channel {UId = uid});

            Node = new MetaIntersection
                       {
                           Name = new FileInfo(rawFile.Path).Directory.Name.Replace(" ", ""),
                           Channels = DataChannels,
                           Intersections = rawFile.GuessIntersections()
                       };

            TimeResolution = rawFile.TimeResolution();

            Start = rawFile.LogRecordTimes().Min();
            Finish = rawFile.LogRecordTimes().Max();

            DeclaredRecordCount = -1;
            DeclaredChannelCount = -1;

            RecordLines = null; //TODO: tmp?

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

            //TODO: fix, headers lie
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