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

        private readonly ulong _channelId;
        private readonly ulong _guildId;

        private static Tuple<string, ulong, ulong> ParseEnvironment()
        {
            var token = Environment.GetEnvironmentVariable("TOKEN");

            if (token == null)
            {
                Console.Error.WriteLine("TOKEN is not defined");
                Environment.Exit(1);
            }


            if (!ulong.TryParse(Environment.GetEnvironmentVariable("GUILDID"), out var guildId))
            {
                Console.Error.WriteLine("GUILDID is not defined");
                Environment.Exit(1);
            }
            
            if (!ulong.TryParse(Environment.GetEnvironmentVariable("CHANNELID"), out var channelId))
            {
                Console.Error.WriteLine("CHANNELID is not defined");
                Environment.Exit(1);
            }

            return new Tuple<string, ulong, ulong>(token, guildId, channelId);
        }

        private Program()
        {
            (_token, _guildId, _channelId) = ParseEnvironment();
            
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
            var guild = _client.GetGuild(_guildId);
            var hasUser = guild.Channels.Where(c => c is SocketVoiceChannel).SelectMany(x => x.Users).Any(user => !user.IsSelfDeafened);

            if (!hasUser) return;
            
            var str = $"Pause {eventArgs.DateTime:HH:mm} - {eventArgs.DateTime + eventArgs.TimeSpan:HH:mm}";
            await Console.Out.WriteLineAsync(str);
            await ((SocketTextChannel) _client.GetChannel(_channelId)).SendMessageAsync(str);
        }

        private Task OnReadyAsync()
        {
            _readyEvent.Set();
            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Channel.Id != _channelId) return;
            
            
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