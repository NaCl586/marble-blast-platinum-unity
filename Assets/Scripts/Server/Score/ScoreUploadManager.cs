using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Server.API;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using Server.Replay;
using UnityEngine;

namespace Server.Score
{
    public class ScoreUploadManager
    {
        private readonly ScoreApi _scoreApi;
        private readonly ScoreQueue _scoreQueue;
        private readonly ReplayUploadManager _replayUpload;

        public ScoreUploadManager(
            ScoreApi scoreApi,
            ScoreQueue scoreQueue,
            ReplayUploadManager replayUpload)
        {
            _scoreApi = scoreApi;
            _scoreQueue = scoreQueue;
            _replayUpload = replayUpload;
        }

        public void QueueScore(
            PendingScore score)
        {
            if (score == null)
                throw new ArgumentNullException(
                    nameof(score));

            _scoreQueue.Enqueue(score);
        }

        public int PendingScoreCount =>
            _scoreQueue.Count;

        public async UniTask ProcessPendingScoresAsync()
        {
            int? userId =
                OnlineManager.Instance.Auth.UserId;

            if (!userId.HasValue)
            {
                Debug.LogWarning(
                    "Cannot process pending scores: UserId unavailable.");

                return;
            }

            while (true)
            {
                PendingScore score =
                    _scoreQueue.PeekForUser(userId.Value);

                if (score == null)
                    break;

                try
                {
                    Debug.Log(
                        $"Submitting pending score: " +
                        $"UserId={score.UserId}, " +
                        $"Level={score.Level}, " +
                        $"TimeMs={score.TimeMs}");

                    SubmitScoreResponse response =
                        await _scoreApi.SubmitScoreAsync(
                            new SubmitScoreRequest
                            {
                                Level = score.Level,
                                TimeMs = score.TimeMs
                            });

                    Debug.Log(
                        $"Pending score submitted. " +
                        $"ScoreId={response.ScoreId}, " +
                        $"PB={response.IsNewPersonalBest}, " +
                        $"WR={response.IsWorldRecord}");

                    // Remove THIS specific score.
                    _scoreQueue.Remove(score);

                    if (response.IsWorldRecord)
                    {
                        await HandleWorldRecord(
                            score,
                            response);
                    }
                    else
                    {
                        DeletePendingReplay(score);
                    }
                }
                catch (Exception ex)
                {
                    score.RetryCount++;

                    _scoreQueue.Update(score);

                    Debug.LogError(
                        $"Pending score submission failed. " +
                        $"UserId={score.UserId}, " +
                        $"Level={score.Level}, " +
                        $"RetryCount={score.RetryCount}");

                    Debug.LogException(ex);

                    // Keep this score in the queue.
                    throw;
                }
            }
        }

        private async UniTask HandleWorldRecord(
            PendingScore score,
            SubmitScoreResponse response)
        {
            if (string.IsNullOrWhiteSpace(
                    score.ReplayFileName))
            {
                Debug.LogError(
                    $"Pending score became a World Record, " +
                    $"but no replay file exists. " +
                    $"Level={score.Level}, " +
                    $"ScoreId={response.ScoreId}");

                return;
            }

            string filePath =
                ReplayPaths.GetAbsolutePath(
                    score.ReplayFileName);

            if (!File.Exists(filePath))
            {
                Debug.LogError(
                    $"Pending WR replay file not found: " +
                    $"{filePath}");

                return;
            }

            PendingReplay pendingReplay =
                new PendingReplay
                {
                    UserId = score.UserId,
                    ScoreId = response.ScoreId,
                    Level = score.Level,
                    TimeMs = response.TimeMs,
                    FileName = score.ReplayFileName,
                    RetryCount = 0
                };

            Debug.Log(
                $"Pending score became a World Record. " +
                $"Creating pending replay. " +
                $"ScoreId={response.ScoreId}");

            _replayUpload.QueueReplay(
                pendingReplay);

            await _replayUpload
                .UploadPendingReplayAsync();
        }

        private void DeletePendingReplay(
            PendingScore score)
        {
            if (string.IsNullOrWhiteSpace(
                    score.ReplayFileName))
            {
                return;
            }

            string filePath =
                ReplayPaths.GetAbsolutePath(
                    score.ReplayFileName);

            if (!File.Exists(filePath))
                return;

            File.Delete(filePath);

            Debug.Log(
                $"Deleted replay for non-WR pending score: " +
                $"{filePath}");
        }
    }
}