using System;

namespace Server.Replay
{
    [Serializable]
    public class PendingReplay
    {
        public int UserId;
        public int ScoreId;
        public string Level = "";
        public int TimeMs;
        public string FileName = "";
        public int RetryCount;
    }
}