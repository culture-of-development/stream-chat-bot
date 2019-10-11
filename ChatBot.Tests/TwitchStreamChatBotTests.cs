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
            Assert.True(template.Contains("tbdgamer hosted with 0 viewers!"));
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
            Assert.True(template.Contains("TODO"));
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
            Assert.True(template.Contains("TODO"));
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
            Assert.True(template.Contains("TODO"));
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
            Assert.True(template.Contains("TODO"));
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
            Assert.True(template.Contains("TODO"));
        }
    }
}
