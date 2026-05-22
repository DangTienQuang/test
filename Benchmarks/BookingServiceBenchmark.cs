using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class BookingServiceBenchmark
    {
        private List<BookingMock> _existingBookings;
        private List<TimeSlotMock> _allSlots;

        public class BookingMock
        {
            public DateTime ScheduledTime { get; set; }
        }

        public class TimeSlotMock
        {
            public TimeSpan StartTime { get; set; }
            public int MaxCapacity { get; set; }
        }

        [Params(10, 100, 1000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            _allSlots = new List<TimeSlotMock>();
            for (int i = 0; i < 24; i++)
            {
                _allSlots.Add(new TimeSlotMock { StartTime = TimeSpan.FromHours(i), MaxCapacity = 5 });
            }

            _existingBookings = new List<BookingMock>();
            var random = new Random(42);
            var today = DateTime.Today;
            for (int i = 0; i < N; i++)
            {
                int hour = random.Next(0, 24);
                _existingBookings.Add(new BookingMock { ScheduledTime = today.AddHours(hour) });
            }
        }

        [Benchmark(Baseline = true)]
        public int CountInLoop()
        {
            int totalFull = 0;
            foreach (var slot in _allSlots)
            {
                var bookedCount = _existingBookings.Count(b => b.ScheduledTime.TimeOfDay == slot.StartTime);
                if (bookedCount >= slot.MaxCapacity)
                {
                    totalFull++;
                }
            }
            return totalFull;
        }

        [Benchmark]
        public int GroupBeforeLoop()
        {
            int totalFull = 0;
            var bookingCountsByTime = _existingBookings
                .GroupBy(b => b.ScheduledTime.TimeOfDay)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var slot in _allSlots)
            {
                bookingCountsByTime.TryGetValue(slot.StartTime, out var bookedCount);
                if (bookedCount >= slot.MaxCapacity)
                {
                    totalFull++;
                }
            }
            return totalFull;
        }
    }
}
