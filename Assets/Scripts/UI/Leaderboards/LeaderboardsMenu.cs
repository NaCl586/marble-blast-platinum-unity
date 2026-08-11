using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Server;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

public class LeaderboardsMenu : MonoBehaviour
{
    public static LeaderboardsMenu Instance;

    [Header("Buttons")]
    public Button logout;
    public Button play;

    [Header("Play Mission Window")]
    public GameObject playMissionWindow;
    public GameObject raycastBlocker;

    [Header("Menu")]
    public GameObject gameWindow;
    public GameObject loadingMenu;
    public GameObject errorMenu;
    public GameObject blackout;

    [Header("Blackout")]
    [SerializeField] private float blackoutDuration = 0.5f;

    [Header("Error")]
    public TextMeshProUGUI errorTitle;
    public TextMeshProUGUI errorMessage;
    public Button yahooButton;
    public ErrorSound errorSound;

    [Header("Loading")]
    public TextMeshProUGUI loadingMessage;

    private bool isProcessing;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        JukeboxManager.instance.PlayMusic("Flanked");

        play.onClick.AddListener(OnPlayClicked);
        logout.onClick.AddListener(OnLogoutClicked);

        yahooButton.onClick.AddListener(OnCloseErrorClicked);

        InitializeUI();

        ReplayRecorder.loadReplay = false;

        if (PlayMissionManager.LevelLoadedFromLeaderboards)
            StartCoroutine(FromGame()); 
    }

    IEnumerator FromGame()
    {
        PlayMissionManager.LevelLoadedFromLeaderboards = false;
        OnPlayClicked();
        blackout.SetActive(true);

        yield return new WaitForSeconds(blackoutDuration);

        blackout.SetActive(false);
    }

    private void InitializeUI()
    {
        loadingMenu.SetActive(false);
        errorMenu.SetActive(false);
        blackout.SetActive(false);

        playMissionWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }

    // --------------------------------------------------
    // PLAY
    // --------------------------------------------------

    private void OnPlayClicked()
    {
        if (isProcessing)
            return;

        playMissionWindow.SetActive(true);
        raycastBlocker.SetActive(true);
    }

    // --------------------------------------------------
    // LOGOUT
    // --------------------------------------------------

    private async void OnLogoutClicked()
    {
        if (isProcessing)
            return;

        if (OnlineManager.Instance == null)
        {
            ShowError(
                "Logout Failed",
                "Online services are unavailable.");

            return;
        }

        isProcessing = true;
        gameWindow.SetActive(false);

        ShowLoading("Logging out...");

        try
        {
            OnlineManager.Instance.Auth.Logout();

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(blackoutDuration));

            HideLoading();
            blackout.SetActive(true);

            await UniTask.Delay(
                TimeSpan.FromSeconds(blackoutDuration));

            ReplayRecorder.leaderboardRecording = false;
            JukeboxManager.instance.PlayMusic("Pianoforte");
            SceneManager.LoadScene("MainMenu");
        }
        catch (Exception ex)
        {
            isProcessing = false;

            HideLoading();

            ShowError(
                "Logout Failed",
                GetErrorMessage(ex));
        }
    }

    // --------------------------------------------------
    // LOADING
    // --------------------------------------------------

    public void ShowLoading(
        string message)
    {
        loadingMessage.text = message;

        loadingMenu.SetActive(true);

        errorMenu.SetActive(false);

        playMissionWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }

    private void HideLoading()
    {
        loadingMenu.SetActive(false);
    }

    // --------------------------------------------------
    // ERROR
    // --------------------------------------------------

    private void ShowError(
        string title,
        string message)
    {
        errorSound.PlayErrorSound();

        errorTitle.text = title;
        errorMessage.text = message;

        errorMenu.SetActive(true);

        loadingMenu.SetActive(false);
    }

    private void OnCloseErrorClicked()
    {
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator ReturnToMainMenu()
    {
        errorMenu.SetActive(false);
        loadingMenu.SetActive(false);

        blackout.SetActive(true);

        yield return new WaitForSeconds(blackoutDuration);

        if (OnlineManager.Instance != null)
            OnlineManager.Instance.Shutdown();

        ReplayRecorder.leaderboardRecording = false;
        SceneManager.LoadScene("MainMenu");
    }

    private string GetErrorMessage(
        Exception ex)
    {
        if (ex == null)
            return "An unknown error occurred.";

        if (!string.IsNullOrWhiteSpace(ex.Message))
            return ex.Message;

        return "An unknown error occurred.";
    }

    // --------------------------------------------------
    // PLAY MISSION WINDOW
    // --------------------------------------------------

    public void ClosePMG()
    {
        if (isProcessing)
            return;

        playMissionWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }
}