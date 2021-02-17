using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace break_bot
{
    internal class Program
    {
        private readonly string _token;
        private readonly DiscordSocketClient _client;

        private readonly ManualResetEvent _onReadyEvent = new ManualResetEvent(false);

        private readonly Timer _timer;

        private int _lastHour = 0;

        private Program()
        {
            _token = Environment.GetEnvironmentVariable("TOKEN") ?? throw new InvalidOperationException();
            _timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, new TimeSpan(0, 0, 30));
            _client = new DiscordSocketClient();

            _client.Log += message => Console.Out.WriteLineAsync(message.ToString());
            _client.Ready += OnReadyAsync;
        }

        private void TimerCallback(object? state)
        {
            var now = DateTime.Now;
            var howLong = 0;
            if (now.Minute == 0)
            {
                switch (now.Hour)
                {
                    case 10:
                    case 11:
                    case 14:
                        howLong = 5;
                        break;
                    case 12:
                        howLong = 30;
                        break;
                }
            }

            if (howLong == 0) return;

            bool foundUser = false;

            foreach (var guild in _client.Guilds)
            foreach (var channel in guild.Channels)
                if (channel is SocketVoiceChannel voiceChannel)
                    foreach (var user in voiceChannel.Users)
                    {
                        if (user.IsSelfDeafened) continue;
                        foundUser = true;
                        goto stopLoop;
                    }

            stopLoop:
            if (foundUser && _lastHour != now.Hour)
            {
                ((SocketTextChannel) _client.GetChannel(811515354169344001)).SendMessageAsync(
                    $"Pause {now.Hour:00}:{now.Minute:00} - {now.Hour:00}:{howLong:00}").GetAwaiter().GetResult();
                _lastHour = now.Hour;
            }
        }

        private static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task OnReadyAsync()
        {
            // Start timer
            _timer.Change(TimeSpan.Zero, new TimeSpan(0, 0, 5));
        }

        private async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();


            await Task.Delay(Timeout.Infinite);
        }
    }
}