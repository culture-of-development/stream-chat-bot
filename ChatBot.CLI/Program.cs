using System;

namespace ChatBot.CLI
{
    class Program
    {
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

            Console.ReadLine();
        }
    }
}
