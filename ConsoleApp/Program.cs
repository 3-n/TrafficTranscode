using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

            var folderName = "folder";

            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            var a = @"ab\cd";
            Console.WriteLine(a.Replace('\\', '-'));

            var now = DateTime.Now.ToString("yyyyMMdd_hh-mm-ss");
            Console.WriteLine(now);
            Console.WriteLine(now.IndexOf('\\'));
            File.WriteAllText(String.Format("{0}\\{1}.txt", "folder", now), "abc");

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

            sw.Stop();
            Console.WriteLine("RECORDS EXTRACTED AT {0}s", sw.Elapsed.TotalSeconds);
            Console.ReadKey();
            sw.Start();

            var f = 0;
            var fileSw = new Stopwatch();
            fileSw.Start();

            foreach (var dataFile in loader.DataFiles)
            {

                enumerable = dataFile.Records as Record[] ?? dataFile.Records.ToArray();

                var sb = new StringBuilder();
                int i = 0;

                sb.Append(String.Format("\t\t\t\t"));

                foreach (var ch in dataFile.DataChannels)
                {
                    sb.Append(String.Format("{0}\t", ch.UId));
                }

                sb.Append(Environment.NewLine);

                foreach (var time in enumerable.Select(r => r.Start).Distinct().OrderBy(dt => dt))
                {
                    sb.Append(String.Format("{0}:\t", time.ToString("MM-dd hh:mm")));
                    foreach (var ch in dataFile.DataChannels)
                    {
                        DateTime time1 = time;
                        Channel ch1 = ch;
                        sb.Append(String.Format("{0}\t\t", enumerable
                            .Where(r => r.Start == time1 && r.Channel == ch1)
                            .ToLine()));
                        //if (i++ % 1000 == 0) Console.WriteLine(i);
                    }

                    sb.Append(Environment.NewLine);
                }

                File.WriteAllText(String.Format("{0}\\{1}.txt", "folder", dataFile), sb.ToString());
                f++;

                Console.WriteLine("{0}/{1} files, {2}s (+{3}s) elapsed...", f, loader.DataFiles.Count, sw.Elapsed.TotalSeconds, fileSw.Elapsed.TotalSeconds);
                fileSw.Restart();
            }

            Console.WriteLine("FILES WRITTEN AT {0}s", sw.ElapsedMilliseconds / 1000);

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


            //return String.Format("{0}({1}) {2}",
            return String.Format("{2}",
                                 start,
                                 duration,
                                 traffs);
           // return enumerable.Count().ToString();
        }
    }


}
