using System;
using System.Collections.Generic;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;

namespace ChatBot
{
    public class TwitchStreamChatBot
    {
        TwitchClient client;
        TwitchAPI api;

        Dictionary<string, Channel> teamMembers;
        string teamName;
        HashSet<string> announcedTeamMembers;

        public void Initialize(string username, string accessToken, string apiClientId, string apiAccessToken, string channel, string teamUrlSlug)
        {
            InitializeTwitchAPI(apiClientId, apiAccessToken);
            InitializeTeam(teamUrlSlug, channel);
            InitializeTwitchClient(username, accessToken, channel);
        }

        public void ConnectChat()
        {
            client.Connect();
        }

        private void InitializeTwitchClient(string username, string accessToken, string channel)
        {
            ConnectionCredentials credentials = new ConnectionCredentials(username, accessToken);

            client = new TwitchClient();
            client.Initialize(credentials, channel);

            client.OnLog += Client_OnLog;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;
        }

        private void InitializeTwitchAPI(string clientId, string accessToken)
        {
            api = new TwitchAPI();
                
            api.Settings.ClientId = clientId;
            api.Settings.AccessToken = accessToken;
        }

        private void InitializeTeam(string teamUrlSlug, string channel)
        {
            var teamTask = api.V5.Teams.GetTeamAsync(teamUrlSlug);
            var team = teamTask.GetAwaiter().GetResult();

            announcedTeamMembers = new HashSet<string>();
            teamName = team.DisplayName;
            teamMembers = new Dictionary<string, Channel>();
            foreach(var user in team.Users)
            {
                teamMembers.Add(user.Id, user);
                if (user.DisplayName == channel)
                {
                    announcedTeamMembers.Add(user.Id);
                }
            }

            Console.WriteLine($"Identified {teamMembers.Count} members on the {teamName} team.");
        }
  
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }
  
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }
  
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var userId = e.ChatMessage.UserId;
            if (teamMembers.ContainsKey(userId) && !announcedTeamMembers.Contains(userId))
            {
                var username = e.ChatMessage.Username;
                var channelLink = teamMembers[userId].Url;
                client.SendMessage(e.ChatMessage.Channel, $"Welcome {username} from the {teamName} team!  They are awesome and you should check out their channel at {channelLink}");
                announcedTeamMembers.Add(userId);
            }
        }
    }
}
