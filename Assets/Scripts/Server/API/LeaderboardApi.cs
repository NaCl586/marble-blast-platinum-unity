using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Server.DTOs.Responses;

namespace Server.API
{
    public class LeaderboardApi
    {
        private readonly ApiClient _client;

        public LeaderboardApi(ApiClient client)
        {
            _client = client;
        }

        public UniTask<LeaderboardResponse> GetLeaderboardAsync(
            string level,
            int page = 1,
            int pageSize = 10)
        {
            string encodedLevel =
                UnityWebRequest.EscapeURL(level);

            return _client.GetAsync<LeaderboardResponse>(
                $"/api/leaderboard?level={encodedLevel}&page={page}&pageSize={pageSize}");
        }
    }
}