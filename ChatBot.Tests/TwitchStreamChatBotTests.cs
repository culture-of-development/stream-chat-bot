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
            string chatMessageRaw = "TODO";
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
