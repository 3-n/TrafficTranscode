using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TrafficTranscode.MetaNet;
using TrafficTranscode.RealNet;

namespace TrafficTranscode.Parse
{
    public static class ParseHelp
    {
        public static string[] LineSeparators = new[] {"\r", "\n"};
        public static string[] WordSeparators = new[] { " ", "\t"};

        //TODO: using some buffered data (in a file)
        public static IEnumerable<Intersection> GuessIntersections(this RawFile rawFile)
        {
            return new List<Intersection>();
        }

        public static TimeSpan TimeResolution(this RawFile rawFile)
        {
            switch (rawFile.Type)
            {
                case FileType.Registry:
                    var resLineSplat = rawFile.LineStarting("Okres").Words();
                    TimeSpan unit;

                    switch (resLineSplat.Last())
                    {
                        case "minut":
                            unit = TimeSpan.FromMinutes(1.0);
                            break;
                        case "godzina":
                            unit = TimeSpan.FromHours(1.0);
                            break;
                        default:
                            throw new NotSupportedException("Unknown time unit, or parsing oversimplication.");
                    }

                    var number = Int32.Parse(resLineSplat[resLineSplat.Length - 2]);
                    return TimeSpan.FromTicks(number*unit.Ticks);

                case FileType.Log:

                    var dateTimes = rawFile.LogRecordTimes();

                    var spans = new List<TimeSpan>();

                    for (int i = 1; i < dateTimes.Count(); i++)
                    {
                        spans.Add(dateTimes[i]- dateTimes[i-1]);
                    }

                    var theSpan = spans.Distinct().ToArray();

                    if(theSpan.Count() > 1)
                    {
                        var histogram = theSpan.Select(s => spans.Count(ts => ts == s));
                        var peak = histogram.Max();

                        var noPeakHistogram = histogram.Except(new[] {peak});



                        //if (noPeakHistogram.Any(h => h > peak / 10) || noPeakHistogram.Count()<histogram.Count()-1)
                        //{
                        //    throw new ParseException(String.Format("Resolution of file not constant: {0}", rawFile.Path));
                        //}

                        return theSpan.OrderByDescending(s => spans.Count(ts => ts == s)).First();
                    }

                    return theSpan.First();
                default:
                    throw new NotImplementedException();
            }

        }

        public static DateTime[] LogRecordTimes(this RawFile rawFile)
        {
            var tables = rawFile.LogFileTableStrings().Select(s => s.LogFileTable());
            var dateRows = tables.SelectMany(t => t.First().Skip(1));
            var timeRows = tables.SelectMany(t => t.Skip(1).First().Skip(1));

            return dateRows
                .Zip(timeRows, (dateStr, timeStr) => DateTime.Parse(String.Format("2012-{0}T{1}:00", dateStr, timeStr)))
                .ToArray();

        }


        // this fixes table headers to be start-time-, not interval-based
        public static string[][] LogFileTable(this string rawFileChunk)
        {
            return rawFileChunk
                .Replace("+", "")
                .Replace("|", "")
                .Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Contains("---"))
                .Select(line => line.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries))
                .Where(line => line.Length > 0)
                .Select(line => (line.First() == "m-d" || line.First() == "h:m") ? line.Take(line.Count() - 1).ToArray() : line)
                .ToArray();
        }

        public static IEnumerable<string> LogFileTableStrings(this RawFile rawFile)
        {
            var border = rawFile.Contents.IndexOf('+');
            var ret = rawFile.Contents
                .Substring(border, rawFile.Contents.Length-border)
                .SplitRegex(@"(?<=\-+\+)[^\+\-\|]+(?=\+\-+)", StringSplitOptions.RemoveEmptyEntries, RegexOptions.Multiline);
            return ret;
        }

        public static string[] SplitRegex(this string input, string pattern, StringSplitOptions stringOptions, RegexOptions regexOptions = RegexOptions.None)
        {
            return Regex.Split(input, pattern, regexOptions)
                .Where(s => stringOptions != StringSplitOptions.RemoveEmptyEntries || !String.IsNullOrEmpty(s))
                .ToArray();
        }

        public static string[] Words(this string line)
        {
            return line.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        public static IEnumerable<string> Lines(this RawFile rawFile, Func<string, bool> predicate = null)
        {
            if(predicate == null)
            {
                predicate = s => true;
            }

            return rawFile
                .Contents
                .Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Where(predicate);
        }

        public static string Line(this RawFile rawFile, Func<string, bool> predicate)
        {
            return rawFile
                .Lines(predicate)
                .First();
        }

        public static IEnumerable<string> LinesStarting(this RawFile rawFile, string beginning)
        {
            return rawFile
                .Lines(line => line.StartsWith(beginning));
        }

        public static string LineStarting(this RawFile rawFile, string beginning)
        {
            return rawFile
                .LinesStarting(beginning)
                .First();
        }

        public static DateTime DateTimeParse(string date, string time)
        {
            return DateTime.Parse(String.Format("20{0}T{1}", date, time));
        }
    }
}