namespace Server.DTOs.Responses
{
    [System.Serializable]
    public class ScoreResponse
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public int TimeMs { get; set; }
    }
}

