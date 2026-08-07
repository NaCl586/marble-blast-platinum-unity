using Cysharp.Threading.Tasks;
using Server.API;

namespace Server.Replay
{
    public class ReplayUploadManager
    {
        private readonly ReplayApi _replayApi;
        private readonly ReplayQueue _replayQueue;

        public ReplayUploadManager(
            ReplayApi replayApi,
            ReplayQueue replayQueue)
        {
            _replayApi = replayApi;
            _replayQueue = replayQueue;
        }

        public void QueueReplay(
            PendingReplay replay)
        {
            _replayQueue.Enqueue(replay);
        }

        public async UniTask UploadPendingReplayAsync()
        {
            // Akan diimplementasikan pada commit Replay.
            await UniTask.CompletedTask;
        }

        public int PendingReplayCount =>
            _replayQueue.Count;
    }
}