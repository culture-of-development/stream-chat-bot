using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace ChatBot
{
    public class TwitchStreamChatBot
    {
        TwitchClient client;
        TwitchAPI api;

        Dictionary<string, Channel> teamMembers = new Dictionary<string, Channel>();
        string myUserId;
        string teamName;
        int approximateCurrentViewerCount = 0;
        HashSet<string> announcedTeamMembers = new HashSet<string>();

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
            // TODO figure out if this is capturing an accurate representation of the current viewer count
            // this is used for the raid out event where we track how many people we send somewhere else
            client.OnExistingUsersDetected += Client_OnExistingUsersDetected;
        }

        private void InitializeTwitchAPI(string clientId, string accessToken)
        {
            api = new TwitchAPI();
                
            api.Settings.ClientId = clientId;
            api.Settings.AccessToken = accessToken;

            var userIdTask = api.Helix.Users.GetUsersAsync(accessToken: accessToken);
            var response = userIdTask.GetAwaiter().GetResult();
            if (response.Users.Any())
            {
                myUserId = response.Users[0].Id;
                Console.WriteLine($"******My user id is: {myUserId} ********");
            }
        }

        private void InitializeTeam(string teamUrlSlug, string channel)
        {
            var teamTask = api.V5.Teams.GetTeamAsync(teamUrlSlug);
            var team = teamTask.GetAwaiter().GetResult();

            teamName = team.DisplayName;
            foreach(var user in team.Users)
            {
                teamMembers.Add(user.Id, user);
            }
            if (myUserId != null)
            {
                announcedTeamMembers.Add(myUserId);
            }

            Console.WriteLine($"Identified {teamMembers.Count} members on the {teamName} team.");
        }
  
        public void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }
  
        public void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }
  
        public void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            AnnounceTeamMember(e.ChatMessage);
            RecordBitsEvent(e.ChatMessage);
            CaptureRaidOut(e.ChatMessage);
            CaptureFollow(e.ChatMessage);
        }

        private void AnnounceTeamMember(ChatMessage message)
        {
            var userId = message.UserId;
            if (teamMembers.ContainsKey(userId) && !announcedTeamMembers.Contains(userId))
            {
                var username = message.Username;
                var channelLink = teamMembers[userId].Url;
                client.SendMessage(message.Channel, $"Welcome @{username} from the {teamName} team!  They are awesome and you should check out their channel at {channelLink}");
                lock(announcedTeamMembers)
                {
                    announcedTeamMembers.Add(userId);
                }
            }
        }

        private void RecordBitsEvent(ChatMessage message)
        {
            if (message.Bits <= 0) return;
            var info = new CheerInfo()
            {
                Channel = message.Username,
                EventTime = DateTime.UtcNow,
                Bits = message.Bits,
            };
            lock(cheers)
            {
                cheers.Add(info);
            }
        }

        private void CaptureRaidOut(ChatMessage message)
        {
            const string raidPrefix = "/raid ";
            //if (message.UserId != myUserId || !message.Message.StartsWith(raidPrefix)) return;
            if (message.UserId != "61809127" || !message.Message.StartsWith(raidPrefix)) return;
            endOfStreamRaid = new RaidInfo
            {
                Channel = message.Message.Substring(raidPrefix.Length),
                EventTime = DateTime.UtcNow,
                ViewerCount = approximateCurrentViewerCount,
            };
        }

        private static Regex followMessageFormat = new Regex("Welcome to the class (?<username>[^!]+)!");
        private void CaptureFollow(ChatMessage message)
        {
            const string streamElementsBotUserId = "100135110";
            if (message.UserId != streamElementsBotUserId || !followMessageFormat.IsMatch(message.Message)) return;
            var info = new FollowerInfo 
            {
                UserDisplayName = followMessageFormat.Match(message.Message).Groups["username"].Value,
                EventTime = DateTime.UtcNow,
            };
            lock(follows)
            {
                var username = info.UserDisplayName;
                if (follows.ContainsKey(username))
                {
                    follows[username] = info;
                }
                else
                {
                    follows.Add(username, info);
                }
            }
        }

        RaidInfo endOfStreamRaid;
        public RaidInfo EndOfStreamRaid => endOfStreamRaid;
        Dictionary<string, RaidInfo> raids = new Dictionary<string, RaidInfo>();
        public IReadOnlyDictionary<string, RaidInfo> Raids => raids;
        Dictionary<string, HostInfo> hosts = new Dictionary<string, HostInfo>();
        public IReadOnlyDictionary<string, HostInfo> Hosts => hosts;
        List<SubscriptionInfo> subs = new List<SubscriptionInfo>();
        public IReadOnlyList<SubscriptionInfo> Subs => subs;
        List<CheerInfo> cheers = new List<CheerInfo>();
        public IReadOnlyList<CheerInfo> Cheers => cheers;
        Dictionary<string, FollowerInfo> follows = new Dictionary<string, FollowerInfo>();
        public IReadOnlyDictionary<string, FollowerInfo> Follows => follows;

        public void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
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

        public void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
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

        public void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
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

        public void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
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

        public void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
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

        public void Client_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
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

        public void Client_OnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            approximateCurrentViewerCount = e.Users.Count;
        }

        public void WriteMarkdownTemplate(string filename)
        {
            var sb = PopulateMarkdownTemplate();
            File.WriteAllText(filename, sb.ToString());
        }

        public StringBuilder PopulateMarkdownTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"---
title: 'S0000 - TODO: stream title'
author: Nick Larsen
categories: streams
date: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
youtube_url: https://youtu.be/TODO
youtube_embed: https://www.youtube.com/embed/TODO
---");
            sb.AppendLine();
            sb.AppendLine("TODO: stream notes about what you actually accomplished");
            sb.AppendLine();

            // TODO: get clips
            // TODO: get markers

            if (raids.Count > 0 || subs.Count > 0 || hosts.Count > 0 || cheers.Count > 0 || follows.Count > 0)
            {
                sb.AppendLine("## Today's Supporters");
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

                if (cheers.Count > 0)
                {
                    sb.AppendLine("### Cheers");
                    sb.AppendLine();
                    foreach(var cheer in cheers.OrderBy(m => m.EventTime))
                    {
                        sb.AppendLine($"- {cheer.EventTime}: {cheer.Channel} cheered with {cheer.Bits.ToString("#,#")} bits!");
                    }
                    sb.AppendLine();
                }

                // follows
                if (follows.Count > 0)
                {
                    sb.AppendLine("### Followers");
                    sb.AppendLine();
                    foreach(var follower in follows.Select(m => m.Value).OrderBy(m => m.EventTime))
                    {
                        sb.AppendLine($"- {follower.EventTime}: {follower.UserDisplayName}");
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
                        sb.AppendLine($"- {raid.EventTime}: [{raid.Channel}](//twitch.tv/{raid.Channel}) raided with {raid.ViewerCount} viewers!");
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
                        sb.AppendLine($"- {host.EventTime}: [{host.Channel}](//twitch.tv/{host.Channel}) hosted with {host.ViewerCount} viewers!");
                    }
                    sb.AppendLine();
                }
            }

            if (endOfStreamRaid != null) {
                sb.AppendLine("## Pay it forward");
                sb.AppendLine();
                var viewerCountText = endOfStreamRaid.ViewerCount > 0 ? $" with {endOfStreamRaid.ViewerCount} viewers" : "";
                sb.AppendLine($"- {endOfStreamRaid.EventTime}: we raided [{endOfStreamRaid.Channel}](//twitch.tv/{endOfStreamRaid.Channel}){viewerCountText}!");
                sb.AppendLine();
            }
            
            return sb;
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
