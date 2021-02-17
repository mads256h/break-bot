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

        private CancellationTokenSource _cancellationTokenSource;

        private SortedList<DateTime, TimeSpan> _breaks;

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
                out var dateTime);
            timeSpan = dateTime.TimeOfDay;
            return r;
        }

        public bool AddBreak(DateTime date, TimeSpan timeSpan)
        {
            if (date < DateTime.Now) return false;
            
            var couldAdd = _breaks.TryAdd(date, timeSpan);
            if (couldAdd) CancelToken();

            return couldAdd;
        }

        public bool RemoveBreak(DateTime date)
        {
            if (date < DateTime.Now) return false;
            
            var couldRemove = _breaks.Remove(date);
            if (couldRemove) CancelToken();

            return couldRemove;
        }
        
        // Cancellation callbacks are ran synchronously so we do this to stop locking up the message received handler.
        private void CancelToken()
        {
            new Thread(() => { _cancellationTokenSource.Cancel(); }).Start();
        }

        // Get a string which is a list of breaks to send as a message.
        public string GetBreaks()
        {
            var sb = new StringBuilder("```\n");
            foreach (var (date, time) in _breaks) sb.AppendLine($"{date:yyyy-MM-dd HH:mm} - {date + time:HH:mm}");

            sb.Append("```");

            return sb.ToString();
        }

        private void RemovePassedBreaks()
        {
            var breaksToRemove = _breaks.Select(keyPair => keyPair.Key).Where(date => date < DateTime.Now)
                .ToImmutableList();

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
                // If we have no more breaks for today add the default breaks for tomorrow.
                // This is broken if the user adds a break that is not today, but its not possible right now.
                if (_breaks.Count == 0) _breaks = GetDefaultBreaks(DateTime.Today + new TimeSpan(1, 0, 0, 0));

                var (date, timeSpan) = GetNextBreak();

                var timeToNextBreak = TimeToNextBreak(date);

                // If a break is added or removed stop the delay, and find the closest break again.
                try
                {
                    Console.WriteLine(timeToNextBreak);
                    await Task.Delay(timeToNextBreak, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
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