using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using Server.Exceptions;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ServerTest : MonoBehaviour
{
    #region Configuration

    [Header("Credentials")]

    [SerializeField]
    private string username = "Gerson";

    [SerializeField]
    private string password = "123456";

    [SerializeField]
    private string invalidPassword = "123456789";

    [SerializeField]
    private string invalidUsername = "UnknownUser";

    [Header("Level")]

    [SerializeField]
    private string level =
        "missions_mbp/beginner/Let'sRoll!";

    [SerializeField]
    private int page = 1;

    [SerializeField]
    private int pageSize = 10;

    #endregion

    [Header("Execution")]

    [SerializeField]
    private bool runOnStart = true;

    [SerializeField]
    private bool stopOnFailure = true;

    [SerializeField]
    private int testId = TEST_ALL;

    protected const int TEST_ALL = 999;

    protected class TestCase
    {
        public int Id;
        public string Name;
        public Func<UniTask> Action;

        public TestCase(
            int id,
            string name,
            Func<UniTask> action)
        {
            Id = id;
            Name = name;
            Action = action;
        }
    }

    private readonly List<TestCase> _tests =
        new List<TestCase>();

    private readonly List<string> _passed =
        new List<string>();

    private readonly List<string> _failed =
        new List<string>();

    private Stopwatch _stopwatch;

    private async void Start()
    {
        RegisterTests();

        if (!runOnStart)
            return;

        await Execute();
    }

    private void RegisterTests()
    {
        // Authentication

        _tests.Add(new TestCase(
            0,
            "Login Success",
            TestLoginSuccess));

        _tests.Add(new TestCase(
            1,
            "Wrong Password",
            TestWrongPassword));

        _tests.Add(new TestCase(
            2,
            "Unknown User",
            TestUnknownUser));

        _tests.Add(new TestCase(
            3,
            "Logout",
            TestLogout));

        // Score

        _tests.Add(new TestCase(
            100,
            "Submit Score",
            TestSubmitScore));

        _tests.Add(new TestCase(
            101,
            "Invalid Score",
            TestInvalidScore));

        _tests.Add(new TestCase(
            102,
            "Better Score",
            TestBetterScore));

        _tests.Add(new TestCase(
            103,
            "Worse Score",
            TestWorseScore));

        _tests.Add(new TestCase(
            104,
            "Unauthorized Submit",
            TestUnauthorizedSubmit));

        // Leaderboard

        _tests.Add(new TestCase(
            200,
            "Leaderboard",
            TestLeaderboard));

        // Replay

        _tests.Add(new TestCase(
            300,
            "Replay Upload",
            TestReplayUpload));

        _tests.Add(new TestCase(
            301,
            "Replay Download",
            TestReplayDownload));
    }

    private async UniTask Login()
    {
        await OnlineManager.Instance.Auth.LoginAsync(
            username,
            password,
            false);
    }

    private void Logout()
    {
        OnlineManager.Instance.Auth.Logout();
    }

    private UniTask<SubmitScoreResponse> Submit(
    int time)
    {
        return OnlineManager.Instance.Score.SubmitScoreAsync(
            new SubmitScoreRequest
            {
                Level = level,
                TimeMs = time
            });
    }

    private UniTask<LeaderboardResponse> GetLeaderboard()
    {
        return OnlineManager.Instance
            .Leaderboard
            .GetLeaderboardAsync(
                level,
                page,
                pageSize);
    }

    protected void AssertResponse(
    SubmitScoreResponse response)
    {
        AssertNotNull(
            response,
            "Response is null.");

        AssertTrue(
            response.TimeMs > 0,
            "Invalid score.");
    }

    private UniTask TestReplayUpload()
    {
        Debug.Log(
            "Replay upload not implemented.");

        return UniTask.CompletedTask;
    }

    private UniTask TestReplayDownload()
    {
        Debug.Log(
            "Replay download not implemented.");

        return UniTask.CompletedTask;
    }

    protected async UniTask AssertThrows<T>(
    Func<UniTask> action)
    where T : Exception
    {
        try
        {
            await action();   // <-- panggil delegate

            throw new Exception(
                $"Expected {typeof(T).Name} but no exception was thrown.");
        }
        catch (Exception ex)
        {
            if (ex is T)
                return;

            throw new Exception(
                $"Expected {typeof(T).Name}, got {ex.GetType().Name}");
        }
    }

    private async UniTask Execute()
    {
        _passed.Clear();
        _failed.Clear();

        _stopwatch = Stopwatch.StartNew();

        if (testId == TEST_ALL)
        {
            foreach (TestCase test in _tests)
            {
                bool success =
                    await RunTest(test);

                if (!success && stopOnFailure)
                    break;
            }
        }
        else
        {
            TestCase test =
                _tests.Find(x => x.Id == testId);

            if (test == null)
            {
                Debug.LogError($"Unknown Test ID {testId}");
                return;
            }

            await RunTest(test);
        }

        _stopwatch.Stop();

        PrintSummary();
    }

    private async UniTask<bool> RunTest(
    TestCase test)
    {
        Debug.Log("");

        Debug.Log(
            $"========== TEST {test.Id} ==========");

        Debug.Log(test.Name);

        Stopwatch sw =
            Stopwatch.StartNew();

        try
        {
            await test.Action();

            sw.Stop();

            Debug.Log(
                $"PASS ({sw.ElapsedMilliseconds} ms)");

            _passed.Add(test.Name);

            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();

            Debug.LogError(
                $"FAIL ({sw.ElapsedMilliseconds} ms)");

            Debug.LogException(ex);

            _failed.Add(test.Name);

            return false;
        }
    }

    private void PrintSummary()
    {
        Debug.Log("");

        Debug.Log(
            "========== SERVER TEST SUMMARY ==========");

        foreach (string name in _passed)
        {
            Debug.Log(
                $"PASS  {name}");
        }

        foreach (string name in _failed)
        {
            Debug.LogError(
                $"FAIL  {name}");
        }

        Debug.Log("");

        Debug.Log(
            $"Passed : {_passed.Count}");

        Debug.Log(
            $"Failed : {_failed.Count}");

        Debug.Log(
            $"Elapsed : {_stopwatch.Elapsed}");

        Debug.Log(
            "=========================================");
    }

    protected void AssertTrue(
    bool condition,
    string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    protected void AssertFalse(
        bool condition,
        string message)
    {
        if (condition)
            throw new Exception(message);
    }

    protected void AssertEqual<T>(
        T expected,
        T actual,
        string message = "")
    {
        if (!EqualityComparer<T>.Default.Equals(
                expected,
                actual))
        {
            throw new Exception(
                $"{message}\nExpected : {expected}\nActual : {actual}");
        }
    }

    protected void AssertNotNull(
        object obj,
        string message)
    {
        if (obj == null)
            throw new Exception(message);
    }

    private async UniTask TestLoginSuccess()
    {
        await Login();

        AssertTrue(
            OnlineManager.Instance.Auth.IsLoggedIn,
            "User should be logged in.");
    }

    private async UniTask TestWrongPassword()
    {
        await AssertThrows<UnauthorizedException>(
            async () =>
            {
                await OnlineManager.Instance.Auth.LoginAsync(
                    username,
                    invalidPassword,
                    false);
            });
    }

    private async UniTask TestBetterScore()
    {
        await Login();

        SubmitScoreResponse first =
            await Submit(15000);

        Debug.Log($"First: PB={first.IsNewPersonalBest}, Time={first.TimeMs}");

        SubmitScoreResponse second =
            await Submit(10000);

        Debug.Log($"Second: PB={second.IsNewPersonalBest}, Time={second.TimeMs}");

        AssertTrue(
            second.IsNewPersonalBest,
            "Should become new PB.");
    }

    private async UniTask TestLogout()
    {
        await Login();

        Logout();

        AssertFalse(
            OnlineManager.Instance.Auth.IsLoggedIn,
            "Logout failed.");
    }

    private async UniTask TestSubmitScore()
    {
        await Login();

        SubmitScoreResponse response =
            await Submit(10000);

        AssertResponse(response);
    }

    private async UniTask TestInvalidScore()
    {
        await Login();

        await AssertThrows<ValidationException>(
            async () =>
            {
                await Submit(-1);
            });
    }

    private async UniTask TestUnknownUser()
    {
        await AssertThrows<UnauthorizedException>(
            async () =>
            {
                await OnlineManager.Instance.Auth.LoginAsync(
                    invalidUsername,
                    password,
                    false);
            });
    }

    private async UniTask TestWorseScore()
    {
        await Login();

        await Submit(10000);

        SubmitScoreResponse response =
            await Submit(15000);

        AssertFalse(
            response.IsNewPersonalBest,
            "Should not become PB.");
    }

    private async UniTask TestUnauthorizedSubmit()
    {
        await Login();

        Logout();

        await AssertThrows<UnauthorizedException>(
            async () =>
            {
                await Submit(10000);
            });
    }

    private async UniTask TestLeaderboard()
    {
        await Login();

        LeaderboardResponse response =
            await GetLeaderboard();

        AssertNotNull(
            response,
            "Leaderboard is null.");

        AssertNotNull(
            response.Scores,
            "Score list is null.");

        foreach (ScoreResponse score in response.Scores)
        {
            Debug.Log(
                $"{score.Rank}. {score.PlayerName} ({score.TimeMs})");
        }
    }
}