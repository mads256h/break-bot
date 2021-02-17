using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace break_bot
{
    public sealed class Scheduler
    {
        public delegate Task OnBreakCallback(SchedulerEventArgs eventArgs);

        private SortedList<DateTime, TimeSpan> _breaks;

        private CancellationTokenSource _cancellationTokenSource;

        public Scheduler()
        {
            _breaks = GetDefaultBreaks(DateTime.Today);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public event OnBreakCallback OnBreak = null!;

        public static bool FromString(string str, out DateTime date)
        {
            return DateTime.TryParseExact(str, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out date);
        }

        public static bool FromString(string str, out TimeSpan timeSpan)
        {
            
            var r = DateTime.TryParseExact(str, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces,
                out DateTime dateTime);
            timeSpan = dateTime.TimeOfDay;
            return r;
        }
        
        public bool AddBreak(DateTime date, TimeSpan timeSpan)
        {
            var couldAdd = _breaks.TryAdd(date, timeSpan);
            if (couldAdd)
            {
                CancelToken();
            }

            return couldAdd;
        }

        public bool RemoveBreak(DateTime date)
        {
            var couldRemove = _breaks.Remove(date);
            if (couldRemove)
            {
                CancelToken();
            }

            return couldRemove;
        }

        private void CancelToken()
        {
            Task.Run(() => { _cancellationTokenSource.Cancel(); });
        }

        public string GetBreaks()
        {
            var sb = new StringBuilder("```\n");
            foreach (var (date, time) in _breaks)
            {
                sb.AppendLine($"{date:yyyy-MM-dd HH:mm} - {date + time:HH:mm}");
            }

            sb.Append("```");

            return sb.ToString();
        }
        
        private void RemovePassedBreaks()
        {
            var breaksToRemove = _breaks.Select(keyPair => keyPair.Key).Where(date => date < DateTime.Now).ToImmutableList();

            foreach (var date in breaksToRemove) _breaks.Remove(date);

            foreach (var (date, _) in _breaks) Console.WriteLine(date);
        }

        private KeyValuePair<DateTime, TimeSpan> GetNextBreak()
        {
            return _breaks.First();
        }

        private static TimeSpan TimeToNextBreak(DateTime breakDateTime)
        {
            return breakDateTime - DateTime.Now;
        }

        public async Task Start()
        {
            RemovePassedBreaks();
            while (true)
            {
                if (_breaks.Count == 0)
                {
                    _breaks = GetDefaultBreaks(DateTime.Today + new TimeSpan(1, 0, 0, 0));
                }
                
                var (date, timeSpan) = GetNextBreak();

                var timeToNextBreak = TimeToNextBreak(date);

                // If a break is added or removed stop the delay, and find the closest break again.
                try
                {
                    await Task.Delay(timeToNextBreak, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    continue;
                }

                await OnBreak(new SchedulerEventArgs(date, timeSpan));

                _breaks.Remove(date);
            }
        }


        private static SortedList<DateTime, TimeSpan> GetDefaultBreaks(DateTime date)
        {
            return new SortedList<DateTime, TimeSpan>
            {
                {date + new TimeSpan(10, 00, 00), new TimeSpan(00, 05, 00)},
                {date + new TimeSpan(11, 00, 00), new TimeSpan(00, 05, 00)},
                {date + new TimeSpan(12, 00, 00), new TimeSpan(00, 30, 00)},
                {date + new TimeSpan(14, 00, 00), new TimeSpan(00, 05, 00)}
            };
        }
    }

    public sealed class SchedulerEventArgs : EventArgs
    {
        public SchedulerEventArgs(DateTime dateTime, TimeSpan timeSpan)
        {
            DateTime = dateTime;
            TimeSpan = timeSpan;
        }

        public DateTime DateTime { get; }
        public TimeSpan TimeSpan { get; }
    }
}