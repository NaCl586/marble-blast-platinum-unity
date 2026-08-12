using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Server;
using Server.API;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using Server.Replay;
using UnityEngine;

namespace Server.Score
{
    public class OnlineScoreManager
    {
        private readonly ScoreApi _scoreApi;
        private readonly ReplayUploadManager _replayUpload;
        private readonly ScoreUploadManager _scoreUpload;

        public OnlineScoreManager(
            ScoreApi scoreApi,
            ScoreUploadManager scoreUpload,
            ReplayUploadManager replayUpload)
        {
            _scoreApi = scoreApi;
            _scoreUpload = scoreUpload;
            _replayUpload = replayUpload;
        }

        public async UniTask SubmitScoreAsync(
            string level)
        {
            if (OnlineManager.Instance == null ||
                !OnlineManager.Instance.Auth.IsLoggedIn)
            {
                Debug.Log(
                    "Player is offline. " +
                    "Score will not be submitted.");

                return;
            }

            int? userId =
                OnlineManager.Instance.Auth.UserId;

            if (!userId.HasValue)
            {
                Debug.LogError(
                    "Cannot submit score. " +
                    "Authenticated UserId is unavailable.");

                return;
            }

            string? username =
                OnlineManager.Instance.Auth.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                Debug.LogError(
                    "Cannot submit score. " +
                    "Username is unavailable.");

                return;
            }

            // Get the actual time from the replay.
            int replayTimeMs;

            string replayPath =
                ReplayRecorder.Instance.SavePendingReplay(
                    username,
                    out replayTimeMs);

            string replayFileName =
                Path.GetRelativePath(
                    ReplayPaths.PendingDirectory,
                    replayPath);

            Debug.Log(
                $"Replay saved before score submission: " +
                $"{replayFileName}, " +
                $"FinalTimeMs={replayTimeMs}");

            try
            {
                Debug.Log(
                    $"Submitting score: " +
                    $"UserId={userId.Value}, " +
                    $"Level={level}, " +
                    $"TimeMs={replayTimeMs}");

                SubmitScoreResponse response =
                    await _scoreApi.SubmitScoreAsync(
                        new SubmitScoreRequest
                        {
                            Level = level,
                            TimeMs = replayTimeMs
                        });

                Debug.Log(
                    $"Score submitted. " +
                    $"ScoreId={response.ScoreId}, " +
                    $"PB={response.IsNewPersonalBest}, " +
                    $"WR={response.IsWorldRecord}, " +
                    $"Time={response.TimeMs}");

                if (response.IsWorldRecord)
                {
                    await HandleWorldRecordAsync(
                        userId.Value,
                        level,
                        response,
                        replayFileName);
                }
                else
                {
                    DeleteReplay(replayFileName);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Score submission failed. " +
                    $"Creating pending score. " +
                    $"UserId={userId.Value}, " +
                    $"Level={level}, " +
                    $"TimeMs={replayTimeMs}");

                PendingScore pendingScore =
                    new PendingScore
                    {
                        UserId = userId.Value,
                        Level = level,
                        ReplayFileName = replayFileName,
                        RetryCount = 0
                    };

                _scoreUpload.QueueScore(pendingScore);

                Debug.Log(
                    $"Pending score queued. " +
                    $"UserId={pendingScore.UserId}, " +
                    $"Replay={pendingScore.ReplayFileName}");

                Debug.LogException(ex);
            }
        }

        private void DeleteReplay(
            string replayFileName)
        {
            if (string.IsNullOrWhiteSpace(
                    replayFileName))
            {
                return;
            }

            string filePath =
                ReplayPaths.GetAbsolutePath(
                    replayFileName);

            if (!File.Exists(filePath))
                return;

            File.Delete(filePath);

            Debug.Log(
                $"Deleted replay for non-WR score: " +
                $"{filePath}");
        }

        private async UniTask HandleWorldRecordAsync(
            int userId,
            string level,
            SubmitScoreResponse response,
            string replayFileName)
        {
            OnlineManager.Instance.ReplayUpload
                .RemovePendingReplayForUserAndLevel(
                    userId,
                    level);

            PendingReplay pendingReplay =
                new PendingReplay
                {
                    UserId = userId,
                    ScoreId = response.ScoreId,
                    Level = level,
                    FileName = replayFileName,
                    RetryCount = 0
                };

            Debug.Log(
                $"Creating pending WR replay. " +
                $"UserId={userId}, " +
                $"ScoreId={response.ScoreId}, " +
                $"File={replayFileName}");

            _replayUpload.QueueReplay(
                pendingReplay);

            await _replayUpload
                .UploadPendingReplayAsync();
        }
    }
}