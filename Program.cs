using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace break_bot
{
    internal class Program
    {
        private readonly string _token;
        private readonly DiscordSocketClient _client;

        private readonly Scheduler _scheduler;

        private readonly ManualResetEvent _readyEvent = new ManualResetEvent(false);

        private const ulong ChannelId = 811515354169344001;
        private const ulong GuildId = 804075956225703997;


        private Program()
        {
            _token = Environment.GetEnvironmentVariable("TOKEN") ?? throw new InvalidOperationException();
            _scheduler = new Scheduler();
            _scheduler.OnBreak += OnBreakAsync;
            _client = new DiscordSocketClient();

            _client.Log += message => Console.Out.WriteLineAsync(message.ToString());
            _client.Ready += OnReadyAsync;
        }


        private static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task OnBreakAsync(SchedulerEventArgs eventArgs)
        {
            var guild = _client.GetGuild(GuildId);
            var hasUser = guild.Channels.Where(c => c is SocketVoiceChannel).SelectMany(x => x.Users).Any(user => !user.IsSelfDeafened);

            if (!hasUser) return;
            
            var str = $"Pause {eventArgs.DateTime:HH:mm} - {eventArgs.DateTime + eventArgs.TimeSpan:HH:mm}";
            await Console.Out.WriteLineAsync(str);
            await ((SocketTextChannel) _client.GetChannel(ChannelId)).SendMessageAsync(str);
        }

        private Task OnReadyAsync()
        {
            _readyEvent.Set();
            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Channel.Id != ChannelId) return;
            
            
            int argPos = 0;

            if (message.HasStringPrefix("!list", ref argPos))
            {
                await message.Channel.SendMessageAsync(_scheduler.GetBreaks());
            }
            else if (message.HasStringPrefix("!add", ref argPos))
            {
                var split = message.Content.Substring(argPos).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                {
                }
                else
                {
                    if (!Scheduler.FromString(split[0], out DateTime start)) return;
                    if (!Scheduler.FromString(split[1], out TimeSpan length)) return;

                    if (_scheduler.AddBreak(start, length)) await message.AddReactionAsync(new Emoji("✅"));
                }
            }
            else if (message.HasStringPrefix("!remove", ref argPos))
            {
                var str = message.Content.Substring(argPos);

                if (!Scheduler.FromString(str, out DateTime start)) return;

                if (_scheduler.RemoveBreak(start)) await message.AddReactionAsync(new Emoji("✅"));
            }
        }

        private async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            _client.MessageReceived += OnMessageReceived;


            _readyEvent.WaitOne();
            
            await _scheduler.Start();
        }
    }
}