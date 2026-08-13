using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class Mission
{
    public Sprite levelImage;
    public string directory;
    public int levelNumber;
    [Space]
    public int time;
    public string missionName;
    public string levelName;
    [TextArea(2, 10)] public string description;
    [TextArea(2, 10)] public string startHelpText;
    public string artist;
    public int goldTime;
    public int ultimateTime;
    public int alarmTime;
    public string music;
    public string skyboxName;
    public bool hasEgg;
}

public enum Type
{
    none,
    beginner,
    intermediate,
    advanced,
    expert,
    custom
}

public enum Game
{
    none,
    gold,
    platinum
}

public abstract class PlayMissionManager : MonoBehaviour
{
    public List<Mission> missions = new List<Mission>();

    [Header("Common UI References")]
    public Image levelImage;
    public TextMeshProUGUI levelDescriptionText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI timeToQualifyText;

    public GameObject notQualifiedText;
    public GameObject notQualifiedImage;

    public GameObject beginnerButton;
    public GameObject intermediateButton;
    public GameObject advancedButton;
    public GameObject expertButton;
    public GameObject customButton;
    public GameObject switchGameButton;

    public Image eggImage;
    public Sprite egg;
    public Sprite egg_nf;

    public Button prev;
    public Button next;
    public Button play;
    public Button home;

    [Header("Window Panels")]
    public GameObject marbleSelectWindow;
    public GameObject searchWindow;
    public GameObject achievementsWindow;
    public GameObject replayWindow;

    [Header("Window Triggers")]
    public Button marbleSelectButton;
    public Button achievementsButton;
    public Button searchButton;
    public Toggle replayButton;

    [Header("Raycast Blockers")]
    public GameObject raycastBlocker;
    public GameObject raycastBlocker2;

    [Space]
    public bool debug = false;
    public static bool LevelLoadedFromLeaderboards = false;

    [HideInInspector] public int selectedLevelNum;
    public static Type currentlySelectedType = Type.none;
    public static Game selectedGame = Game.none;

    protected virtual bool IsAnyWindowActive()
    {
        return (marbleSelectWindow && marbleSelectWindow.activeSelf) ||
               (searchWindow && searchWindow.activeSelf) ||
               (achievementsWindow && achievementsWindow.activeSelf) ||
               (replayWindow && replayWindow.activeSelf);
    }

    protected virtual void Update()
    {
        if (!IsAnyWindowActive())
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) PrevButton();
            if (Input.GetKeyDown(KeyCode.RightArrow)) NextButton();
        }
    }

    protected virtual void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CloseAllWindows();
        SetBlockerActive(false, false);

        if (marbleSelectButton) marbleSelectButton.onClick.AddListener(() => { SetBlockerActive(true, false); ToggleMarbleSelectWindow(true); });
        if (achievementsButton) achievementsButton.onClick.AddListener(() => { SetBlockerActive(true, false); ToggleAchievementWindow(true); });
        if (searchButton) searchButton.onClick.AddListener(OnSearchButtonClicked);

        if (replayButton)
        {
            replayButton.onValueChanged.AddListener(ToggleReplay);
            replayButton.SetIsOnWithoutNotify(false);
        }

        StartCoroutine(WaitUntilFinishLoading());
    }

    protected virtual void CloseAllWindows()
    {
        ToggleWindow(marbleSelectWindow, false);
        ToggleWindow(searchWindow, false);
        ToggleWindow(achievementsWindow, false);
        ToggleWindow(replayWindow, false);
    }

    protected virtual IEnumerator WaitUntilFinishLoading()
    {
        while (MissionInfo.instance == null || MissionInfo.instance.missionsPlatinumBeginner == null || MissionInfo.instance.missionsPlatinumBeginner.Count == 0)
            yield return null;

        Time.timeScale = 1;

        if (selectedGame == Game.none)
            selectedGame = Game.platinum;

        BindNavigationAndDifficultyButtons();

        if (currentlySelectedType == Type.none)
            currentlySelectedType = Type.beginner;

        LoadMissions(currentlySelectedType, selectedGame);

        SearchManager searchManager = GetComponent<SearchManager>();
        if (searchManager != null) searchManager.InitSearchElements();
    }

    protected virtual void BindNavigationAndDifficultyButtons()
    {
        BindButton(beginnerButton, () => LoadMissions(Type.beginner, selectedGame));
        BindButton(intermediateButton, () => LoadMissions(Type.intermediate, selectedGame));
        BindButton(advancedButton, () => LoadMissions(Type.advanced, selectedGame));
        BindButton(expertButton, () => LoadMissions(Type.expert, Game.platinum));
        BindButton(customButton, () => { 
            if(this is LeaderboardsPlayMission)
                LoadMissions(Type.custom, selectedGame); 
            else
                LoadMissions(Type.custom, Game.gold);
        });

        if (switchGameButton) switchGameButton.GetComponent<Button>()?.onClick.AddListener(SwitchGame);

        if (home) home.onClick.AddListener(OnHomeButtonClicked);
        if (prev) prev.onClick.AddListener(PrevButton);
        if (next) next.onClick.AddListener(NextButton);
        if (play) play.onClick.AddListener(OnPlayButtonClicked);
    }

    protected virtual void OnHomeButtonClicked() => SceneManager.LoadScene("MainMenu");
    protected virtual void OnPlayButtonClicked() 
    {
        if (OnlineManager.Instance == null || !OnlineManager.Instance.Auth.IsLoggedIn)
        {
            LevelLoadedFromLeaderboards = false;
            SceneManager.LoadScene("Loading");
        }
        else
        {
            LeaderboardsMenu lm = GetComponent<LeaderboardsMenu>();
            LevelLoadedFromLeaderboards = true;
            CheckMission(lm).Forget();
        }
    }

    async UniTask CheckMission(LeaderboardsMenu lm)
    {
        JukeboxManager.instance.ForceStop();

        lm.blackout.SetActive(true);
        lm.ShowLoading("Checking Mission Consistency...");

        await UniTask.Delay(
            TimeSpan.FromSeconds(1));

        string missionPath =
            MissionInfo.instance.MissionPath;

        try
        {
            List<string> files =
                DataIntegrityManager.GetMissionIntegrityFiles(
                    missionPath
                );

            if (files.Count == 0)
            {
                throw new Exception(
                    "Could not read the mission integrity data."
                );
            }

            IntegrityResponse response =
                await OnlineManager.Instance
                    .Integrity
                    .CheckAsync(
                        new IntegrityRequest
                        {
                            GameVersion =
                                Application.version,

                            Files = files
                        }
                    );

            List<string> invalidFiles =
                DataIntegrityManager.VerifyAgainstServer(
                    response
                );

            if (invalidFiles.Count > 0)
            {
                string modifiedFiles =
                    string.Join(
                        "\n",
                        invalidFiles
                    );

                lm.ShowError(
                    "Invalid game data",
                    "It seems that internal game data was modified in some way. " +
                    "If either you modified any files, or it was done by any virus, " +
                    "please ask the forums for the original data or reinstall MBP.\n\n" +
                    "Modified file(s):\n" +
                    modifiedFiles,
                    true
                );

                Debug.LogError(
                    $"[Integrity] Mission consistency check failed!\n" +
                    $"Mission: {missionPath}\n" +
                    $"Invalid file(s):\n{modifiedFiles}"
                );

                return;
            }

            SceneManager.LoadScene("Loading");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[Integrity] Integrity check failed:\n{ex}"
            );

            lm.ShowError(
                "Integrity check failed",
                "The game could not verify the mission data with the server.\n\n" +
                "Please make sure you are connected to the internet and try again.",
                true
            );
        }
    }

    protected virtual void OnSearchButtonClicked()
    {
        SetBlockerActive(true, false);
        ToggleSearchWindow(true);

        SearchManager searchManager = GetComponent<SearchManager>();
        if (searchManager != null)
        {
            searchManager.SelectFirstButton();
            if (searchManager.scrollRect != null)
                searchManager.scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public virtual void SwitchGame()
    {
        selectedGame = (selectedGame == Game.gold) ? Game.platinum : Game.gold;
        currentlySelectedType = Type.beginner;
        LoadMissions(Type.beginner, selectedGame);
    }

    public virtual void LoadMissions(Type difficulty, Game game)
    {
        selectedGame = game;
        currentlySelectedType = difficulty;

        missions = GetMissionsList(difficulty, selectedGame);
        UpdateUIForGameAndDifficulty();
        SetLevelInfo(ClampSelectedLevel());
    }

    protected virtual List<Mission> GetMissionsList(Type difficulty, Game game)
    {
        if (game == Game.gold)
        {
            switch (difficulty)
            {
                case Type.beginner: return MissionInfo.instance.missionsGoldBeginner;
                case Type.intermediate: return MissionInfo.instance.missionsGoldIntermediate;
                case Type.advanced: return MissionInfo.instance.missionsGoldAdvanced;
                case Type.custom: return MissionInfo.instance.missionsGoldCustom;
            }
        }
        else if (game == Game.platinum)
        {
            switch (difficulty)
            {
                case Type.beginner: return MissionInfo.instance.missionsPlatinumBeginner;
                case Type.intermediate: return MissionInfo.instance.missionsPlatinumIntermediate;
                case Type.advanced: return MissionInfo.instance.missionsPlatinumAdvanced;
                case Type.expert: return MissionInfo.instance.missionsPlatinumExpert;
                case Type.custom: return MissionInfo.instance.missionsGoldCustom;
            }
        }
        return new List<Mission>();
    }

    protected abstract void UpdateUIForGameAndDifficulty();

    public virtual void SetLevelInfo(int number)
    {
        selectedLevelNum = number;

        if (missions == null || missions.Count == 0)
        {
            HandleEmptyMissionList();
            return;
        }

        Mission mission = missions[number];
        int qualifiedLevel = GetQualifiedLevel();

        if (play) play.interactable = (qualifiedLevel >= number);
        if (prev) prev.interactable = (number > 0);
        if (next) next.interactable = (number < missions.Count - 1);

        int lastQualifiedLevel = Mathf.Min(number, qualifiedLevel);
        PlayerPrefs.SetInt($"SelectedLevel{CapitalizeFirst(currentlySelectedType.ToString())}{CapitalizeFirst(selectedGame.ToString())}", lastQualifiedLevel);

        if (levelDescriptionText)
        {
            levelDescriptionText.gameObject.SetActive(true);
            levelDescriptionText.text = $"{mission.description}\n<b>Author:</b> {mission.artist}";
            RefreshTMPLayout(levelDescriptionText);
        }

        if (timeToQualifyText)
        {
            timeToQualifyText.text = mission.time != -1 ? $"Par Time: {Utils.FormatTime(mission.time)}" : string.Empty;
            RefreshTMPLayout(timeToQualifyText);
        }

        if (currentLevelText)
            currentLevelText.text = $"{mission.levelName} - {CapitalizeFirst(currentlySelectedType.ToString())} Level {number + 1}";

        if (levelImage)
        {
            levelImage.sprite = mission.levelImage;
            levelImage.color = mission.levelImage != null ? Color.white : Color.clear;
        }

        if (notQualifiedImage) notQualifiedImage.SetActive(qualifiedLevel < number);
        if (notQualifiedText) notQualifiedText.SetActive(qualifiedLevel < number);

        if (eggImage)
        {
            eggImage.gameObject.SetActive(mission.hasEgg);
            if (mission.hasEgg)
            {
                bool hasFoundEgg = PlayerPrefs.GetInt(mission.levelName + "_EasterEgg", 0) == 1;
                eggImage.sprite = hasFoundEgg ? egg : egg_nf;
            }
        }

        SetMissionInfo(mission);
        UpdateMissionSpecificUI(number);
    }

    protected abstract void UpdateMissionSpecificUI(int levelIndex);
    protected abstract void HandleEmptyMissionList();

    protected virtual int GetQualifiedLevel()
    {
        if (debug || currentlySelectedType == Type.custom) return 9999;
        return PlayerPrefs.GetInt($"QualifiedLevel{CapitalizeFirst(currentlySelectedType.ToString())}{CapitalizeFirst(selectedGame.ToString())}", 0);
    }

    private int ClampSelectedLevel()
    {
        int qualifiedLevel = GetQualifiedLevel();
        int savedLevel = PlayerPrefs.GetInt($"SelectedLevel{CapitalizeFirst(currentlySelectedType.ToString())}{CapitalizeFirst(selectedGame.ToString())}", qualifiedLevel);

        if (savedLevel < 0) return 0;
        if (missions != null && savedLevel >= missions.Count) return Mathf.Max(0, missions.Count - 1);
        return savedLevel;
    }

    public void PrevButton()
    {
        if (selectedLevelNum > 0)
            SetLevelInfo(selectedLevelNum - 1);
    }

    public void NextButton()
    {
        if (selectedLevelNum < missions.Count - 1)
            SetLevelInfo(selectedLevelNum + 1);
    }

    public void ToggleReplay(bool value)
    {
        SetBlockerActive(value, false);
        ToggleReplayWindow(value);

        if (value)
            GetComponent<NewReplayManager>()?.Init();
        else
            ReplayRecorder.recordReplay = false;
    }

    public void ToggleMarbleSelectWindow(bool active) => ToggleWindow(marbleSelectWindow, active);
    public void ToggleSearchWindow(bool active) => ToggleWindow(searchWindow, active);
    public void ToggleReplayWindow(bool active) => ToggleWindow(replayWindow, active);
    public void ToggleAchievementWindow(bool active) => ToggleWindow(achievementsWindow, active);

    protected void ToggleWindow(GameObject window, bool active)
    {
        if (window != null) window.SetActive(active);
    }

    protected void SetBlockerActive(bool active, bool active2)
    {
        if (raycastBlocker) raycastBlocker.SetActive(active);
        if (raycastBlocker2) raycastBlocker2.SetActive(active2);
    }

    protected void SetMissionInfo(Mission mission)
    {
        MissionInfo.instance.MissionPath = mission.directory;
        MissionInfo.instance.missionName = mission.missionName;
        MissionInfo.instance.time = mission.time;
        MissionInfo.instance.levelName = mission.levelName;
        MissionInfo.instance.description = mission.description;
        MissionInfo.instance.startHelpText = mission.startHelpText;
        MissionInfo.instance.level = mission.levelNumber;
        MissionInfo.instance.artist = mission.artist;
        MissionInfo.instance.goldTime = mission.goldTime;
        MissionInfo.instance.ultimateTime = mission.ultimateTime;
        MissionInfo.instance.alarmTime = mission.alarmTime;
        MissionInfo.instance.hasEgg = mission.hasEgg;

        string musicName = mission.music;
        musicName = string.IsNullOrEmpty(musicName) ? string.Empty : Path.GetFileNameWithoutExtension(musicName.Trim()).Replace(".ogg", "");
        MissionInfo.instance.music = musicName;

        string skyboxName = string.IsNullOrEmpty(mission.skyboxName) ? "intermediate_sky" : mission.skyboxName;
        MissionInfo.instance.skybox = Application.CanStreamedLevelBeLoaded(skyboxName) ? skyboxName : "intermediate_sky";
    }

    protected void BindButton(GameObject buttonObj, UnityEngine.Events.UnityAction action)
    {
        if (buttonObj && buttonObj.TryGetComponent<Button>(out var btn))
            btn.onClick.AddListener(action);
    }

    protected void RefreshTMPLayout(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tmp.rectTransform);
    }

    public static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}