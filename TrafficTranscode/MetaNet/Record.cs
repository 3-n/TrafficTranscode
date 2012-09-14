using System;
using System.Linq;
using System.Collections.Generic;
using TrafficTranscode.RealNet;

namespace TrafficTranscode.MetaNet
{
    public class Record
    {
        private TimeSpan duration;
        private DateTime start;
        public decimal Traffic { get; set; }

        public string City { get; set; }
        public MetaIntersection Node { get; set; }
        public DateTime Start
        {
            get { return start; }
            set 
            { 
                start = value;
                RoundTo(Precision);
            }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set
            {
                if (value > TimeSpan.Zero)
                {
                    duration = value;
                }
                RoundTo(Precision);
                if (OriginalDuration > TimeSpan.Zero)
                {
                    OriginalDuration = value;
                }
            }
        }

        public Intersection Intersection { get; set; }
        public Channel Channel { get; set; }

        public int Status { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }

        public TimeSpan OriginalDuration { get; private set; }

        public TimeSpan Precision { get; set; }

        public DateTime End
        {
            get { return Start + Duration; }
        }

        public IEnumerable<Record> Split(int pieceCount = 2)
        {
            if(pieceCount < 1)
            {
                throw new ArgumentOutOfRangeException("pieceCount", pieceCount, "Count cannot be less than one");
            }

            if(pieceCount == 1)
            {
                return new[] { this };
            }

            var pieceLength = Duration.Ticks/pieceCount;

            return Enumerable
                .Range(0, pieceCount)
                .Select(i => new Record(
                    this, 
                    Traffic/pieceCount, 
                    Start + TimeSpan.FromTicks(i*pieceLength),
                    TimeSpan.FromTicks(pieceLength)));
        }

        public IEnumerable<Record> Split(TimeSpan pieceLength)
        {
            if(pieceLength >= Duration)
            {
                return new[] {this};
            }

            return Split((int) Math.Round(Duration.Ticks/(double) pieceLength.Ticks)); //screw it
        }

        public void ResetScaling()
        {
            throw new NotImplementedException();
        }

        public Record()
        {
            Precision = TimeSpan.FromMinutes(1);
        }

        public Record(Record record) : this(record, record.Traffic, record.Start, record.Duration)
        {
        }

        public Record(Record record, decimal traffic, DateTime start, TimeSpan duration) : this()
        {
            Precision = TimeSpan.Zero;
            // "mutable" stuff
            Traffic = traffic;
            Start = start;
            Duration = duration;

            // "immutable" stuff
            City = record.City;
            Node = record.Node;
            Intersection = record.Intersection;
            Channel = record.Channel;
            Status = record.Status;
            Error = record.Error;
            OriginalDuration = record.OriginalDuration;
            Precision = Precision;
        }

        public Record(Record record, decimal traffic, DateTime start, DateTime end) : this(record, traffic, start, end-start)
        {
        }

        public void RoundTo(TimeSpan unit)
        {
            if(unit.Ticks < 100)
            {
                return;
            }

            start = Start.Round(unit);
            duration = Duration.Round(unit);
            
        }

        public decimal TrafficBetween(DateTime from, DateTime to)
        {
            if(from < Start)
            {
                from = Start;
            }
            if (to > End)
            {
                to = End;
            }

            var periodLength = to - from;

            return Traffic*periodLength.Ticks/Duration.Ticks;
        }


    }

    public static class RecordHelp
    {
        public static TimeSpan DefaultUnit = TimeSpan.FromMinutes(1);

        public static IEnumerable<Record> Stitch(this IEnumerable<Record> records, int desiredPieceCount)
        {
            var sortedRecords = records.OrderBy(r => r.Start).ToArray();
            var durationSum = sortedRecords.Select(r => r.Duration).Sum();
            var desiredPieceLength = TimeSpan.FromTicks(durationSum.Ticks/desiredPieceCount);

            var recIndex = 0;
            var timeInto = sortedRecords.First().Start;

            for (int i = 0; i < sortedRecords.Length-1; i++)
            {
                if(sortedRecords[i].End.Round() != sortedRecords[i+1].Start.Round())
                {
                    throw new ArgumentException(String.Format("Records with indexes {0},{1} do not share a common start-end point in time.", i, i+1));
                }
            }

            var newRecords = new List<Record>();

            for (int i = 0; i < desiredPieceCount; i++)
            {
                var newPieceStart = timeInto;
                var firstIndex = recIndex;
                while (timeInto.Round() < sortedRecords[recIndex].End.Round())
                {
                    timeInto += desiredPieceLength;
                    recIndex++;
                }
                var lastIndex = recIndex;

                if(firstIndex==lastIndex)
                {
                    var onlyInvolved = sortedRecords[firstIndex];
                    newRecords.Add(new Record(onlyInvolved, onlyInvolved.TrafficBetween(newPieceStart, timeInto), newPieceStart, timeInto));
                }
                else
                {
                    var firstInvolved = sortedRecords[firstIndex]; 
                    var lastInvolved = sortedRecords[lastIndex];
                    var fullyInvolved = sortedRecords.Take(lastIndex+1).Skip(firstIndex);

                    newRecords.Add(new Record(
                        firstInvolved,
                        fullyInvolved.Sum(r => r.TrafficBetween(newPieceStart, timeInto)),
                        newPieceStart,
                        desiredPieceLength));
                }


            }


            return newRecords;
        }

        public static IEnumerable<Record> Stitch(this IEnumerable<Record> records, TimeSpan? desiredPieceLength = null)
        {
            var sum = records.Select(r => r.Duration).Sum();
            TimeSpan pieceLength = desiredPieceLength ?? sum;

            var exactRatio = (sum.Ticks+10)/(decimal)pieceLength.Ticks;

            var maxPossiblePieceCount = (int)Math.Floor(exactRatio);

            if(maxPossiblePieceCount==0)
            {
                return null;
            }

            return records.Stitch(maxPossiblePieceCount);
        }

        public static Record StitchAll(this IEnumerable<Record> records)
        {
            return records.Stitch().First();
        }

        public static TimeSpan Sum(this IEnumerable<TimeSpan> records)
        {
            if(!records.Any())
            {
                Console.WriteLine("zero");
                return TimeSpan.Zero;
            }
            return records.Aggregate((ts1, ts2) => ts1 + ts2);
        }

        public static DateTime Round(this DateTime dateTime, TimeSpan? roundingUnit = null)
        {
            var unit = roundingUnit ?? DefaultUnit;

            if (unit.Ticks < 10)
            {
                return dateTime;
            }

            var ticks = (dateTime.Ticks + (unit.Ticks / 2) + 1) / unit.Ticks;
            return new DateTime(ticks * unit.Ticks);
        }

        public static TimeSpan Round(this TimeSpan timeSpan, TimeSpan unit)
        {
            if (unit.Ticks < 10)
            {
                return timeSpan;
            }

            var spanTicks = (timeSpan.Ticks + (unit.Ticks / 2) + 1) / unit.Ticks;
            return new TimeSpan(spanTicks * unit.Ticks);
        }
    }
}

