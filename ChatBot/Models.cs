using System;

namespace ChatBot
{
    public class RaidInfo
    {
        public string Channel { get; set; }
        public DateTime EventTime { get; set; }
        public int ViewerCount { get; set; }
    }

    public class FollowerInfo
    {
        public string UserDisplayName { get; set; }
        public DateTime EventTime { get; set; }
    }

    public class CheerInfo
    {
        public string Channel { get; set; }
        public DateTime EventTime { get; set; }
        public int Bits { get; set; }
    }

    public class HostInfo
    {
        public string Channel { get; set; }
        public DateTime EventTime { get; set; }
        public int ViewerCount { get; set; }
    }

    public class SubscriptionInfo
    {
        public string UserDisplayName { get; set; }
        public string GiftedByUserDisplayName { get; set; }
        public int? GiftedCount { get; set; }
        public DateTime EventTime { get; set; }
        public string PlanName { get; set; }
        public string Message { get; set; }
        public int? Months { get; set; }
        public SubscriptionInfoType Type { get; set; }
    }

    public enum SubscriptionInfoType
    {
        Unknown = 0,
        Community,
        Gifted,
        Regular,
    }
}