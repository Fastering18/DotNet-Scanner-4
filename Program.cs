using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DynamicExpresso;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using BlackerzDotNet;

class Program
{
    static void Main(string[] args)
    {
        new Program().MainAsync().GetAwaiter().GetResult();
    }

    private readonly DiscordSocketClient _client;
    private readonly char _prefix;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly APIBot _blackerzbot;

    private Program()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            //MessageCacheSize = 50,

        });
        
        _commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,

            CaseSensitiveCommands = false,
        });

        _prefix = '\\';

        _client.Log += Log;
        _commands.Log += Log;

        _services = ConfigureServices();
        
        _blackerzbot = new APIBot("fUOmjlvBmgDXNYFdqJtNqf", BotID: 839337795382149130);
    }

    private static IServiceProvider ConfigureServices()
    {
        var map = new ServiceCollection();
            // repeat
            //.AddSingleton(new SomeServiceClass());

        return map.BuildServiceProvider();
    }

    private static Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }
        Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
        Console.ResetColor();

        return Task.CompletedTask;
    }

    private async Task MainAsync()
    {
        await InitCommands();

        await _client.LoginAsync(TokenType.Bot,
            Environment.GetEnvironmentVariable("DiscordToken"));
        await _client.StartAsync();

        await _client.SetGameAsync(type: ActivityType.Watching, name: "blackerz.tk (discord.Net)");
         
        await _blackerzbot.Verify();
        
        await Task.Delay(Timeout.Infinite);
    }

    private async Task InitCommands()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        //await _commands.AddModuleAsync<SomeModule>(_services);

        _client.MessageReceived += HandleCommandAsync;
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        var msg = arg as SocketUserMessage;
        if (msg == null) return;
        if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

        int posStart = 1;
        
        if (!(msg.HasCharPrefix(_prefix, ref posStart) || msg.HasMentionPrefix(_client.CurrentUser, ref posStart))) return;

        var context = new SocketCommandContext(_client, msg);
        var result = await _commands.ExecuteAsync(context, posStart, _services);

        string[] args = Regex.Split(msg.Content.Trim().Substring(posStart), " +");
        string command = args.GetValue(0).ToString().ToLower();
        Console.WriteLine(command);

        if (command == "help")
        {
            var embed = new EmbedBuilder()
                .WithColor(new Color(26, 155, 226))
                .WithTitle("Blackerz server manager Scanner 4")
                .WithDescription("Blackerz website data distribution maintained by Ghosteez#5936 using discord.Net")
                .WithThumbnailUrl(thumbnailUrl: _client.CurrentUser.GetAvatarUrl())
                .WithCurrentTimestamp()
                .Build();
            await msg.Channel.SendMessageAsync(embed: embed);
        }
        else if (command == "ping")
        {
            await msg.Channel.SendMessageAsync(_client.Latency.ToString() + "ms");
        }
        else if (command == "eval")
        {
            try
            {
                var interpreter = new Interpreter().SetVariable("client", _client).SetVariable("message", msg);
                Lambda parsedExpression = interpreter.Parse(String.Join(" ", args).Substring(command.Length), new Parameter("pi", typeof(double)));
                var hasileval = parsedExpression.Invoke(3.14);
                await msg.Channel.SendMessageAsync("```csharp\n" + hasileval + "\n```");
            } catch(Exception err)
            {
                await msg.Channel.SendMessageAsync("```csharp\n" + err.Message.Substring(0, err.Message.Length > 1980 ? 1980 : err.Message.Length) + "\n```");
            }
        } else if (command == "botinfo")
        {
            if (args.Length < 2)
            {
                await msg.Channel.SendMessageAsync("No client id present.");
                return;
            }
            string botId = args.GetValue(1).ToString();
            try
            {
                BotData thebot = await BlackerzBot.GetBotData(botId);
                if (thebot == default(BotData))
                {
                    await msg.Channel.SendMessageAsync("No bot found from database.");
                    return;
                }
                Console.WriteLine(thebot.GetInviteLink());
                var embed = new EmbedBuilder()
                .WithColor(new Color(26, 155, 226))
                .WithTitle("**" + thebot.Name + "** Bot")
                .WithDescription("**Bot Info:** " + thebot.Tag + " | `" + thebot.Id + "`\n**Owner:** " + thebot.Owner.Name + " | `" + thebot.Owner.Id + "`\n**Upvotes:** " + thebot.Upvotes + "\n**Invite link:** [Click me](" + thebot.GetInviteLink() + ")\n**Prefix:** `" + thebot.Prefix + "`\n\n" + thebot.ShortDescription)
                .WithThumbnailUrl(thumbnailUrl: BlackerzBot.AvatarOf(thebot.Id, thebot.Avatar))
                .WithCurrentTimestamp()
                .Build();
                await msg.Channel.SendMessageAsync(embed: embed);
            } catch(Exception e)
            {
                Console.WriteLine(e);
                await msg.Channel.SendMessageAsync("Failed to request..");
            }
        } else if (command == "cekvote") 
        {
             Voted VoteResult = await _blackerzbot.CheckUserVote((ulong)msg.Author.Id);
             System.Console.WriteLine("vote: {0}\nvrified: {1}\ndev vrified: {2}", VoteResult, VoteResult.IsVoted, _blackerzbot.IsVerified);
             await msg.Channel.SendMessageAsync(VoteResult.IsVoted == true ? "Your user marked as voted" : "Your user havent voted our bot Scanner 4");
        }

        //if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
        //    await msg.Channel.SendMessageAsync(result.ErrorReason);

    }
}
