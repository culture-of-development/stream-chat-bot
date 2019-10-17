using System;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models.Internal;
using Xunit;

namespace ChatBot.Tests
{
    public class TwitchStreamChatBotTests
    {
        static IrcMessage GetIrcMessage(string rawIrcMessage)
        {
            var ircParserType = typeof(TwitchClient).Assembly.GetType("TwitchLib.Client.Internal.Parsing.IrcParser");
            var ircParser = Activator.CreateInstance(ircParserType, nonPublic: true);
            var parseIrcMessageMethodInfo = ircParserType.GetMethod("ParseIrcMessage");
            var ircMessage = (IrcMessage)parseIrcMessageMethodInfo.Invoke(ircParser, new[] { rawIrcMessage });
            return ircMessage;
        }

        [Fact]
        public void Test_Client_OnMessageWithBits()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "@badge-info=;badges=bits/100;bits=100;color=#FF0000;display-name=tbdgamer;emotes=;flags=;id=306b2ca4-c4ee-4449-84cc-ef5b5cc1c74f;mod=0;room-id=61809127;subscriber=0;tmi-sent-ts=1570725601895;turbo=0;user-id=51497560;user-type= :tbdgamer!tbdgamer@tbdgamer.tmi.twitch.tv PRIVMSG #nick_larsen :Cheer100";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var emoteCollection = new TwitchLib.Client.Models.MessageEmoteCollection();
            var args = new OnMessageReceivedArgs()
            {
                ChatMessage = new TwitchLib.Client.Models.ChatMessage("nick_larsen", ircMessage, ref emoteCollection),
            };
            chatBot.Client_OnMessageReceived(null, args);
            Assert.True(chatBot.Cheers.Count == 1);
            chatBot.Client_OnMessageReceived(null, args);
            Assert.True(chatBot.Cheers.Count == 2);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("tbdgamer cheered with 100 bits!", template);
        }

        [Fact]
        public void Test_Client_OnMessageWithRaidCommand()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "@badge-info=subscriber/6;badges=broadcaster/1,subscriber/0,premium/1;color=;display-name=nick_larsen;emotes=;flags=;id=0e8c15ea-e4db-49cc-8f6f-d867d2675b3b;mod=0;room-id=61809127;subscriber=1;tmi-sent-ts=1571330987242;turbo=0;user-id=61809127;user-type= :nick_larsen!nick_larsen@nick_larsen.tmi.twitch.tv PRIVMSG #nick_larsen :/raid LuckyNoS7evin";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var emoteCollection = new TwitchLib.Client.Models.MessageEmoteCollection();
            var args = new OnMessageReceivedArgs()
            {
                ChatMessage = new TwitchLib.Client.Models.ChatMessage("nick_larsen", ircMessage, ref emoteCollection),
            };
            Assert.True(chatBot.EndOfStreamRaid == null);
            chatBot.Client_OnMessageReceived(null, args);
            Assert.True(chatBot.EndOfStreamRaid.Channel == "LuckyNoS7evin");

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("we raided [LuckyNoS7evin](//twitch.tv/LuckyNoS7evin)", template);
        }

        [Fact]
        public void Test_Client_OnMessageWithFollowerAnnouncement()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "@badge-info=;badges=moderator/1,partner/1;color=#5B99FF;display-name=StreamElements;emotes=;flags=;id=0fce3cfc-04a4-42b2-a981-72cf7c8824a0;mod=1;room-id=61809127;subscriber=0;tmi-sent-ts=1571332723824;turbo=0;user-id=100135110;user-type=mod :streamelements!streamelements@streamelements.tmi.twitch.tv PRIVMSG #nick_larsen :Welcome to the class rexogamerswitch!";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var emoteCollection = new TwitchLib.Client.Models.MessageEmoteCollection();
            var args = new OnMessageReceivedArgs()
            {
                ChatMessage = new TwitchLib.Client.Models.ChatMessage("nick_larsen", ircMessage, ref emoteCollection),
            };
            Assert.True(chatBot.Follows.Count == 0);
            chatBot.Client_OnMessageReceived(null, args);
            Assert.True(chatBot.Follows.Count == 1);
            chatBot.Client_OnMessageReceived(null, args);
            Assert.True(chatBot.Follows.Count == 1);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains(": rexogamerswitch", template);
        }

        [Fact]
        public void Test_Client_OnHosted()
        {
            var chatBot = new TwitchStreamChatBot();
            // TODO: try to get a message that includes viewers
            string chatMessageRaw = ":jtv!jtv@jtv.tmi.twitch.tv PRIVMSG nick_larsen :tbdgamer is now hosting you.";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var args = new OnBeingHostedArgs()
            {
                BeingHostedNotification = new TwitchLib.Client.Models.BeingHostedNotification("nick_larsen", ircMessage),
            };
            chatBot.Client_OnBeingHosted(null, args);
            Assert.True(chatBot.Hosts.Count == 1);
            chatBot.Client_OnBeingHosted(null, args);
            Assert.True(chatBot.Hosts.Count == 1);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("tbdgamer hosted with 0 viewers!", template);
        }

        [Fact]
        public void Test_Client_OnRaidNotification()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "@badge-info=;badges=;color=#FF0000;display-name=LuckyNoS7evin;emotes=;msg-param-viewerCount=4;id=306b2ca4-c4ee-4449-84cc-ef5b5cc1c74f;login=LuckyNoS7evin;mod=0;msg-id=raid;room-id=61809127;subscriber=0;system-msg=;tmi-sent-ts=1570725601895;turbo=0;user-id=51497560;user-type= :tmi.twitch.tv USERNOTICE #nick_larsen :";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var args = new OnRaidNotificationArgs()
            {
                RaidNotificaiton = new TwitchLib.Client.Models.RaidNotification(ircMessage),
            };
            Assert.True(chatBot.EndOfStreamRaid == null);
            chatBot.Client_OnRaidNotification(null, args);
            Assert.True(chatBot.EndOfStreamRaid != null);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("TODO", template);
        }

        [Fact]
        public void Test_Client_OnCommunitySubscription()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "TODO";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var args = new OnCommunitySubscriptionArgs()
            {
                GiftedSubscription = new TwitchLib.Client.Models.CommunitySubscription(ircMessage),
            };
            Assert.True(chatBot.EndOfStreamRaid == null);
            chatBot.Client_OnCommunitySubscription(null, args);
            Assert.True(chatBot.EndOfStreamRaid != null);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("TODO", template);
        }

        [Fact]
        public void Test_Client_OnGiftedSubscription()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "TODO";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var args = new OnGiftedSubscriptionArgs()
            {
                GiftedSubscription = new TwitchLib.Client.Models.GiftedSubscription(ircMessage),
            };
            Assert.True(chatBot.EndOfStreamRaid == null);
            chatBot.Client_OnGiftedSubscription(null, args);
            Assert.True(chatBot.EndOfStreamRaid != null);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("TODO", template);
        }

        [Fact]
        public void Test_Client_OnNewSubscriber()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "TODO";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var args = new OnNewSubscriberArgs()
            {
                Subscriber = new TwitchLib.Client.Models.Subscriber(ircMessage),
            };
            Assert.True(chatBot.EndOfStreamRaid == null);
            chatBot.Client_OnNewSubscriber(null, args);
            Assert.True(chatBot.EndOfStreamRaid != null);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("TODO", template);
        }

        [Fact]
        public void Test_Client_OnReSubscriber()
        {
            var chatBot = new TwitchStreamChatBot();
            string chatMessageRaw = "TODO";
            var ircMessage = GetIrcMessage(chatMessageRaw);
            var args = new OnReSubscriberArgs()
            {
                ReSubscriber = new TwitchLib.Client.Models.ReSubscriber(ircMessage),
            };
            Assert.True(chatBot.EndOfStreamRaid == null);
            chatBot.Client_OnReSubscriber(null, args);
            Assert.True(chatBot.EndOfStreamRaid != null);

            var template = chatBot.PopulateMarkdownTemplate().ToString();
            Assert.Contains("TODO", template);
        }
    }
}
