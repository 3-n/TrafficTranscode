using System;
using System.Collections.Generic;
using System.Linq;
using TrafficTranscode.MetaNet;
using TrafficTranscode.RealNet;

namespace TrafficTranscode.Parse
{
    public static class ParseHelp
    {
        public static string[] LineSeparators = new[] {"\r", "\n"};
        public static string[] WordSeparators = new[] { " " };

        //TODO: using some buffered data (in a file)
        public static IEnumerable<Intersection> GuessIntersections(this RawFile rawFile)
        {
            return new List<Intersection>();
        }

        public static TimeSpan TimeResolution(this RawFile rawFile)
        {
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
    }
}