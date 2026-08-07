using System;

namespace Server.Replay
{
    [Serializable]
    public class PendingReplay
    {
        public int ScoreId;

        public string FileName = "";

        public int RetryCount;

        public long CreatedAtTicks;
    }
}