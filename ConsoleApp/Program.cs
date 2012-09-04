using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrafficTranscode.Parse;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var testPath = @"E:\Politechnika\TWO\MGR\Natężenia\Głogowska - Rynek Łazarski\Poznań, Głogowska-Rynek [248] 12-06-2012.rej";

            var loader = new Loader(testPath);

            var records = loader.Records;

            Console.WriteLine(records.Count());
            Console.ReadKey();

        }
    }
}
