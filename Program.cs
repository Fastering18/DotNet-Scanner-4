using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.RegularExpressions;

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

    }

    private static IServiceProvider ConfigureServices()
    {
        var map = new ServiceCollection();
            // Repeat this for all the service classes
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

        // Login and connect.
        await _client.LoginAsync(TokenType.Bot,
            "ODM5MzM3Nzk1MzgyMTQ5MTMw.YJIMPA.ICLo8LOtQSXjbaZRUdEtX2XwcYQ"
            /*Environment.GetEnvironmentVariable("DiscordToken")*/);
        await _client.StartAsync();

        await _client.SetGameAsync(type: ActivityType.Watching, name: "blackerz.tk (discord.Net)");

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

        //if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
        //    await msg.Channel.SendMessageAsync(result.ErrorReason);

    }
}