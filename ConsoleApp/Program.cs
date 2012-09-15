using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrafficTranscode.MetaNet;
using TrafficTranscode.Parse;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            //var testPath = @"E:\Politechnika\TWO\MGR\Natężenia\Głogowska - Rynek Łazarski\Poznań, Głogowska-Rynek [248] 12-06-2012.rej";
            var testPath = @"E:\Politechnika\TWO\MGR\Natężenia";//\Grochowska - Arciszewskiego";

            var loader = new Loader(testPath);

            Console.WriteLine("LOADED AT {0}s", sw.ElapsedMilliseconds/1000);

            var records = loader.Records;

            File.WriteAllLines("bad_files.txt", ParseDiagnostics.BadFiles);

            var enumerable = records as Record[] ?? records.ToArray();
            Console.WriteLine(enumerable.Count());
            //Console.ReadKey();

            Console.WriteLine("RECORDS EXTRACTED AT {0}s", sw.ElapsedMilliseconds / 1000);

            var sb = new StringBuilder();
            int i = 0;

            sb.Append(String.Format("\t\t\t\t"));

            foreach (var ch in loader.DataFiles.First().DataChannels)
            {
                sb.Append(String.Format("{0}\t", ch.UId));
            }

            sb.Append(Environment.NewLine);

            foreach (var time in enumerable.Select(r => r.Start).Distinct().OrderBy(dt => dt))
            {
                sb.Append(String.Format("{0}:\t", time.ToString("MM-dd hh:mm")));
                foreach (var ch in loader.DataFiles.First().DataChannels)
                {
                    DateTime time1 = time;
                    Channel ch1 = ch;
                    sb.Append(String.Format("{0}\t\t", enumerable
                        .Where(r => r.Start == time1 && r.Channel == ch1)
                        .ToLine()));
                   if(i++%1000==0) Console.WriteLine(i);
                }
                
                sb.Append(Environment.NewLine);
            }

            File.WriteAllText("test.txt", sb.ToString());

            Console.WriteLine("FILE WRITTEN AT {0}s", sw.ElapsedMilliseconds / 1000);

            Console.ReadKey();
        }
    }

    public static class Tmp
    {
        public static string ToLine(this IEnumerable<Record> enumerable)
        {
            var records = enumerable as Record[] ?? enumerable.ToArray();
            var traffs = records.Aggregate("", (current, record) => current + (record.Traffic + " "));

            var start = records.Any() ? records.First().Start : DateTime.MinValue;
            var duration = records.Any() ? records.First().Duration : TimeSpan.Zero;


            return String.Format("{0}({1}) {2}",
                                 start,
                                 duration,
                                 traffs);
           // return enumerable.Count().ToString();
        }
    }


}
