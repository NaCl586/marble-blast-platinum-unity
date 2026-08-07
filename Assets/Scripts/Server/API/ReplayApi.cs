using Cysharp.Threading.Tasks;

namespace Server.API
{
    public class ReplayApi
    {
        private readonly ApiClient _client;

        public ReplayApi(ApiClient client)
        {
            _client = client;
        }
    }
}

