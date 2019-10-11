using System;
using System.Timers;

namespace ChatBot.CLI
{
    class Program
    {
        static Timer outputStreamTemplateTimer;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var channel = "nick_larsen";
            var teamUrlSlug = "livecoders";
            var twitchUsername = "nick_larsen_bot";
            var twitchAccessToken = Environment.GetEnvironmentVariable("nick_larsen_bot_access_token");
            var twitchApiClientId = Environment.GetEnvironmentVariable("nick_larsen_bot_client_id");
            var twitchApiAccessToken = Environment.GetEnvironmentVariable("nick_larsen_bot_api_access_token");
            Console.WriteLine($"There is an access token? {!string.IsNullOrWhiteSpace(twitchAccessToken)}");
            Console.WriteLine($"There is a client id? {!string.IsNullOrWhiteSpace(twitchApiClientId)}");
            Console.WriteLine($"There is an api access token? {!string.IsNullOrWhiteSpace(twitchApiAccessToken)}");
            
            var twitchBot = new TwitchStreamChatBot();
            twitchBot.Initialize(twitchUsername, twitchAccessToken, twitchApiClientId, twitchApiAccessToken, channel, teamUrlSlug);
            twitchBot.ConnectChat();

            string templateFilename = $@"I:\culture-of-development\culture-of-development.github.com\source\_drafts\s0000-{DateTime.UtcNow.ToString("yyyyMMdd")}.md";
            outputStreamTemplateTimer = new Timer(60_000);
            outputStreamTemplateTimer.Elapsed += (o, _) => twitchBot.WriteMarkdownTemplate(templateFilename);
            outputStreamTemplateTimer.AutoReset = true;
            outputStreamTemplateTimer.Start();

            Console.ReadLine();
        }
    }
}
