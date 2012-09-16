using System;
using System.Collections.Generic;
using System.Linq;

namespace TrafficTranscode.Parse
{
    public class LogTable
    {
        public TimeSpan TimeResolution { get; private set; }
        public string[][] Data { get; private set; }

        public LogTable(string[][] table)
        {
            Data = table;



            var spans = RecordSpans().Distinct().ToList();

            if(spans.Count > 1)
            {
                throw new ParseException(String.Format("Resolution of table not constant. Kinda fatal."));
            }

            TimeResolution = spans.First();
        }

        public TimeSpan[] RecordSpans()
        {
            var dateRow = Data.First().Skip(1);
            var timeRow = Data.Skip(1).First().Skip(1);

            var recordTimestamps = dateRow
                .Zip(timeRow, (dateStr, timeStr) => DateTime.Parse(String.Format("2012-{0}T{1}:00", dateStr, timeStr)))
                .ToArray();

            var spans = new List<TimeSpan>();

            for (int i = 1; i < recordTimestamps.Count(); i++)
            {
                spans.Add(recordTimestamps[i] - recordTimestamps[i - 1]);
            }

            return spans.ToArray();
        }
    }
}