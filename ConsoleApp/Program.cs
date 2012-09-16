using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntMicro.Migrant;
using TrafficTranscode.MetaNet;
using TrafficTranscode.Parse;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO: protobuf records
            //TODO: parameters filtering records by
            // - intersection
            // - metaintersection
            // - time (from, to, <=, <)
            // - channels (also for infering from metaintersection, intersection)

            var folderName = "folder";
            var recordsFileName = "records.mig";
            var testPath = @"E:\Politechnika\TWO\MGR\Natężenia";

            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }


            var sw = new Stopwatch();
            sw.Start();

            IEnumerable<Record> records;
            Record[] enumerable;
            Loader loader;

            var serializer = new Serializer();
            serializer.Initialize(typeof(Loader));


            if (!File.Exists(recordsFileName))
            {
                Console.WriteLine("SERIALIZIN'");
                loader = new Loader(testPath);
                Console.WriteLine("LOADER CREATED AT {0:0.00}s", sw.Elapsed.TotalSeconds);
                var fStream = new FileStream(recordsFileName, FileMode.Create);
                serializer.Serialize(loader, fStream);
                Console.WriteLine("LOADER SERIALIZED AT {0:0.00}s", sw.Elapsed.TotalSeconds);
            }
            else
            {
                Console.WriteLine("DESERIALIZIN'");
                var fStream = new FileStream(recordsFileName, FileMode.Open);
                loader = serializer.Deserialize<Loader>(fStream);
                Console.WriteLine("LOADER DESERIALIZED AT {0:0.00}s", sw.Elapsed.TotalSeconds);
            }
            Console.WriteLine("LOADED AT {0:0.00}s", sw.Elapsed.TotalSeconds);

            records = loader.Records;

            File.WriteAllLines("bad_files.txt", ParseDiagnostics.BadFiles);

            enumerable = records as Record[] ?? records.ToArray();
            Console.WriteLine(enumerable.Count());

            sw.Stop();
            Console.WriteLine("RECORDS EXTRACTED AT {0:0.00}s", sw.Elapsed.TotalSeconds);
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

                Console.WriteLine("{0}/{1} files, {2:0.00}s (+{3:0.00}s) elapsed...", f, loader.DataFiles.Count, sw.Elapsed.TotalSeconds, fileSw.Elapsed.TotalSeconds);
                fileSw.Restart();
            }

            Console.WriteLine("FILES WRITTEN AT {0:0.00}s", sw.ElapsedMilliseconds / 1000);

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
