using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            client.OnBeingHosted += Client_OnBeingHosted;
            client.OnRaidNotification += Client_OnRaidNotification;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;
            client.OnCommunitySubscription += Client_OnCommunitySubscription;
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
                client.SendMessage(e.ChatMessage.Channel, $"Welcome @{username} from the {teamName} team!  They are awesome and you should check out their channel at {channelLink}");
                announcedTeamMembers.Add(userId);
            }
        }

        Dictionary<string, RaidInfo> raids = new Dictionary<string, RaidInfo>();
        Dictionary<string, HostInfo> hosts = new Dictionary<string, HostInfo>();
        List<SubscriptionInfo> subs = new List<SubscriptionInfo>();

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            var channel = e.RaidNotificaiton.DisplayName;
            var info = new RaidInfo
            {
                Channel = channel,
                EventTime = DateTime.UtcNow, // TODO: get this from the timestamp
                ViewerCount = int.Parse(e.RaidNotificaiton.MsgParamViewerCount),
            };
            lock(raids)
            {
                if (!raids.ContainsKey(channel))
                {
                    raids.Add(channel, info);
                }
                else
                {
                    raids[channel] = info;
                }
            }
        }

        private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            if (e.BeingHostedNotification.IsAutoHosted) return;
            var channel = e.BeingHostedNotification.HostedByChannel;
            var info = new HostInfo
            {
                Channel = channel,
                EventTime = DateTime.UtcNow, // TODO: get this from the timestamp
                ViewerCount = e.BeingHostedNotification.Viewers,
            };
            lock(hosts)
            {
                if (!hosts.ContainsKey(channel))
                {
                    hosts.Add(channel, info);
                }
                else
                {
                    hosts[channel] = info;
                }
            }
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            var info = new SubscriptionInfo
            {
                UserDisplayName = e.Subscriber.DisplayName,
                PlanName = e.Subscriber.SubscriptionPlanName,
                Message = e.Subscriber.ResubMessage,
                Months = 1,
            };
            OnSub(info);
        }

        private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            var info = new SubscriptionInfo
            {
                UserDisplayName = e.ReSubscriber.DisplayName,
                PlanName = e.ReSubscriber.SubscriptionPlanName,
                Message = e.ReSubscriber.ResubMessage,
                Months = e.ReSubscriber.Months,
            };
            OnSub(info);
        }

        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            var info = new SubscriptionInfo
            {
                UserDisplayName = e.GiftedSubscription.MsgParamRecipientDisplayName,
                GiftedByUserDisplayName = e.GiftedSubscription.DisplayName,
                PlanName = e.GiftedSubscription.MsgParamSubPlanName,
                Months = int.Parse(e.GiftedSubscription.MsgParamMonths),
            };
            OnSub(info);
        }

        private void Client_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {
            var info = new SubscriptionInfo
            {
                GiftedByUserDisplayName = e.GiftedSubscription.DisplayName,
                PlanName = e.GiftedSubscription.MsgParamSubPlan.ToString(),
                GiftedCount = e.GiftedSubscription.MsgParamMassGiftCount,
            };
            OnSub(info);
        }

        private void OnSub(SubscriptionInfo info)
        {
            info.EventTime = DateTime.UtcNow;
            lock(subs)
            {
                subs.Add(info);
            }
        }

        public void WriteMarkdownTemplate(string filename)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Stream Notes for {DateTime.Now.ToShortDateString()}");
            sb.AppendLine();

            if (raids.Count > 0 || subs.Count > 0 || hosts.Count > 0)
            {
                sb.AppendLine("## Supporters");
                sb.AppendLine();

                // subscriptions
                if (subs.Count > 0)
                {
                    sb.AppendLine("### Subscriptions");
                    sb.AppendLine();
                    foreach(var sub in subs.OrderBy(m => m.EventTime))
                    {
                        sb.AppendLine($"- {sub.EventTime}: {FormatSubscription(sub)}");
                    }
                    sb.AppendLine();
                }

                // raids
                if (raids.Count > 0)
                {
                    sb.AppendLine("### Raids");
                    sb.AppendLine();
                    foreach(var raid in raids.Select(m => m.Value).OrderBy(m => m.EventTime))
                    {
                        sb.AppendLine($"- {raid.EventTime}: {raid.Channel} raided with {raid.ViewerCount} viewers!");
                    }
                    sb.AppendLine();
                }

                // hosts
                if (hosts.Count > 0)
                {
                    sb.AppendLine("### Hosts");
                    sb.AppendLine();
                    foreach(var host in hosts.Select(m => m.Value).OrderBy(m => m.EventTime))
                    {
                        sb.AppendLine($"- {host.EventTime}: {host.Channel} hosted with {host.ViewerCount} viewers!");
                    }
                    sb.AppendLine();
                }
            }
        }

        private string FormatSubscription(SubscriptionInfo subInfo)
        {
            // community
            if (subInfo.Type == SubscriptionInfoType.Community)
            {
                return $"{subInfo.GiftedByUserDisplayName} gifted {subInfo.GiftedCount} {subInfo.PlanName} subscriptions!";
            }

            // gifted
            if (subInfo.Type == SubscriptionInfoType.Gifted)
            {
                if (subInfo.Months > 1) {
                    return $"{subInfo.GiftedByUserDisplayName} gifted {subInfo.UserDisplayName} a {subInfo.PlanName} subscription!  They are on a {subInfo.Months} month streak!";
                }
                return $"{subInfo.GiftedByUserDisplayName} gifted {subInfo.UserDisplayName} a {subInfo.PlanName} subscription!";
            }

            // new or re
            if (subInfo.Type == SubscriptionInfoType.Regular)
            {
                var message = string.IsNullOrWhiteSpace(subInfo.Message) ? "" : $"\n  - message: {subInfo.Message}";
                if (subInfo.Months > 1) {
                    return $"{subInfo.UserDisplayName} resubscribed with a {subInfo.PlanName} subscription!  They are on a {subInfo.Months} month streak!{message}";
                }
                return $"{subInfo.UserDisplayName} subscribed with a {subInfo.PlanName} subscription!{message}";
            }

            // todo: don't throw it all away because of one little mistake
            throw new Exception("Unknown subscription type");
        }
    }
}
