using DG.Tweening;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Server;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;
    void Awake()
    {
        instance = this;
        UpdateHUDMaterial();
    }
    [Header("Other References")]
    [SerializeField] Canvas canvas;

    [Space]

    [SerializeField] Sprite[] numbers;
    [SerializeField] Sprite[] numbersGreen;
    [SerializeField] Sprite[] numbersRed;
    [SerializeField] Image[] timerNumbers;
    [SerializeField] TextMeshProUGUI centerText;
    [SerializeField] TextMeshProUGUI bottomText;
    [SerializeField] TextMeshProUGUI fpsText;
    [SerializeField] Texture[] powerupIcon;
    [SerializeField] RawImage powerupHUD;
    [SerializeField] Image[] targetGem;
    [SerializeField] Image[] currentGem;
    [SerializeField] GameObject gemCountUI;
    [SerializeField] GameObject recordingIcon;

    [Space]

    [SerializeField] GameObject readyImage;
    [SerializeField] GameObject setImage;
    [SerializeField] GameObject goImage;
    [SerializeField] GameObject outOfBoundsImage;

    [Space]

    [SerializeField] TextMeshProUGUI lbStatusText;

    [Space]

    public GameObject oobInsultMenu;
    [SerializeField] TextMeshProUGUI oobInsultTitleText;
    [SerializeField] TextMeshProUGUI oobInsultCaptionText;
    [SerializeField] Button oobInsultCloseButton;

    [Space]

    public GameObject saveReplayMenu;
    [SerializeField] TMP_InputField replayMenuName;
    [SerializeField] TMP_InputField replayMenuAuthor;
    [SerializeField] TMP_InputField replayMenuDescription;
    [SerializeField] Button replayMenuApply;
    [SerializeField] Button replayMenuCancel;
    [SerializeField] private Scrollbar scrollbar;
    public ScrollRect scrollRect;
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;
    [SerializeField] private float step = 0.1f;

    [Space]

    [Header("Global Chat")]
    [SerializeField] private GameObject globalChat;
    [SerializeField] private TextMeshProUGUI globalChatText;
    [SerializeField] private TMP_InputField globalChatInput;
    [SerializeField] private GameObject fpsBox;
    [SerializeField] private RectTransform fpsBox_offline;
    [SerializeField] private RectTransform fpsBox_online;

    private const int MaxChatLines = 8;

    private readonly List<string> chatLines =
        new List<string>();

    private bool chatInputOpen;
    public bool IsChatInputOpen => chatInputOpen && !ReplayRecorder.loadReplay && Time.timeScale > 0;

    Tween centerTextFade;
    Tween bottomTextFade;

    Sprite[] timerColor;
    float timer = 0f;

    [HideInInspector] public bool isInitialized = false;

    public void Init()
    {
        timerColor = new Sprite[numbers.Length];
        oobInsultCloseButton.onClick.AddListener(() => {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Time.timeScale = 1;
            oobInsultMenu.SetActive(false);
        });
        isInitialized = true;

        // Listen to text changes
        ReplayRecorder.actualReplayName = string.Empty;
        ReplayRecorder.replayAuthor = string.Empty;
        ReplayRecorder.replayDesc = string.Empty;
        replayMenuName.text = ReplayRecorder.actualReplayName;
        replayMenuAuthor.text = ReplayRecorder.replayAuthor;
        replayMenuDescription.text = ReplayRecorder.replayDesc;
        replayMenuName.onValueChanged.AddListener(SetName);
        replayMenuAuthor.onValueChanged.AddListener(SetAuthor);
        replayMenuDescription.onValueChanged.AddListener(SetDesc);

        // Listen for scrollbar movement (drag, mouse wheel, buttons)
        scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);

        // Initial state
        OnScrollbarValueChanged(scrollbar.value);

        scrollUpButton.onClick.AddListener(ScrollUp);
        scrollDownButton.onClick.AddListener(ScrollDown);

        recordingIcon.SetActive(ReplayRecorder.recordReplay);

        if (OnlineManager.Instance != null &&
            OnlineManager.Instance.Chat != null)
        {
            OnlineManager.Instance.Chat.MessageReceived +=
                OnChatMessageReceived;

            OnlineManager.Instance.Chat.SystemMessageReceived +=
                OnSystemMessageReceived;

            OnlineManager.Instance.Chat.RecentMessagesReceived +=
                OnRecentMessagesReceived;

            LoadChatHistory();
        }

        RectTransform fpsBoxRect =
            fpsBox.GetComponent<RectTransform>();

        if (OnlineManager.Instance != null &&
            OnlineManager.Instance.Chat != null &&
            !ReplayRecorder.loadReplay &&
            !LeaderboardsMenu.ReplayCenterLoadedFromLeaderboards)
        {
            globalChat.SetActive(true);

            fpsBoxRect.anchorMin = fpsBox_online.anchorMin;
            fpsBoxRect.anchorMax = fpsBox_online.anchorMax;
            fpsBoxRect.pivot = fpsBox_online.pivot;
            fpsBoxRect.anchoredPosition = fpsBox_online.anchoredPosition;
            fpsBoxRect.sizeDelta = fpsBox_online.sizeDelta;
            fpsBoxRect.localRotation = fpsBox_online.localRotation;
            fpsBoxRect.localScale = fpsBox_online.localScale;
        }
        else
        {
            globalChat.SetActive(false);

            fpsBoxRect.anchorMin = fpsBox_offline.anchorMin;
            fpsBoxRect.anchorMax = fpsBox_offline.anchorMax;
            fpsBoxRect.pivot = fpsBox_offline.pivot;
            fpsBoxRect.anchoredPosition = fpsBox_offline.anchoredPosition;
            fpsBoxRect.sizeDelta = fpsBox_offline.sizeDelta;
            fpsBoxRect.localRotation = fpsBox_offline.localRotation;
            fpsBoxRect.localScale = fpsBox_offline.localScale;
        }
    }

    public void SetLBStatus(string text)
    {
        Debug.Log(text);
        lbStatusText.text = text;
    }

    public void SaveAndReturn()
    {
        replayMenuApply.onClick.RemoveAllListeners();
        replayMenuApply.onClick.AddListener(() => {
            ReplayRecorder.Instance.SaveReplay();
            Debug.Log("Replay Saved");

            if (OnlineManager.Instance == null || !OnlineManager.Instance.Auth.IsLoggedIn)
            {
                JukeboxManager.instance.PlayMusic("Pianoforte");
                SceneManager.LoadScene("PlayMission");
            }
            else
            {
                JukeboxManager.instance.PlayMusic("Flanked");
                SceneManager.LoadScene("LBPlayMission");
            }
        });

        replayMenuCancel.onClick.RemoveAllListeners();
        replayMenuCancel.onClick.AddListener(() => {
            Debug.Log("Replay Not Saved");

            if (OnlineManager.Instance == null || !OnlineManager.Instance.Auth.IsLoggedIn)
            {
                JukeboxManager.instance.PlayMusic("Pianoforte");
                SceneManager.LoadScene("PlayMission");
            }
            else
            {
                JukeboxManager.instance.PlayMusic("Flanked");
                SceneManager.LoadScene("LBPlayMission");
            }
        });
    }

    public void SaveAndRetry()
    {
        replayMenuApply.onClick.RemoveAllListeners();
        replayMenuApply.onClick.AddListener(() => {
            ReplayRecorder.Instance.SaveReplay();
            Debug.Log("Replay Saved");

            GameManager.instance?.ReplayLevel();
        });

        replayMenuCancel.onClick.RemoveAllListeners();
        replayMenuCancel.onClick.AddListener(() => {
            Debug.Log("Replay Not Saved");

            GameManager.instance?.ReplayLevel();
        });
    }

    public void SetName(string s)
    {
        ReplayRecorder.actualReplayName = s;
    }
    public void SetAuthor(string s)
    {
        ReplayRecorder.replayAuthor = s;
    }
    public void SetDesc(string s)
    {
        Canvas.ForceUpdateCanvases();
        ReplayRecorder.replayDesc = s;
    }

    private void Update()
    {
        if (fpsText)
        {
            timer += Time.unscaledDeltaTime;

            if (timer >= 0.5f)
            {
                fpsText.text = "FPS: " + RoundSmart((float)(1 / Time.unscaledDeltaTime));
                timer = 0f;
            }
        }

        if(!ReplayRecorder.loadReplay && Time.timeScale > 0)
            HandleChatInput();
    }

    private void HandleChatInput()
    {
        if (!chatInputOpen)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                OpenChatInput();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendChatInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelChatInput();
            return;
        }
    }

    float RoundSmart(float value)
    {
        int decimals = Mathf.Abs(value) >= 1000f ? 0 : 1;
        return (float)System.Math.Round(value, decimals, System.MidpointRounding.AwayFromZero);
    }

    public void SetOutOfBoundsMessage(int oobCount, string message)
    {
        oobInsultMenu.SetActive(true);
        Time.timeScale = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        oobInsultTitleText.text = "Out of Bounds " + oobCount + " times";
        oobInsultCaptionText.text = message;
    }

    public void UpdateHUDMaterial()
    {
        int targetLayer = LayerMask.NameToLayer("HUD");
        float smoothness01 = 1f;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].layer != targetLayer)
                continue;

            Renderer[] renderers = allObjects[i].GetComponentsInChildren<Renderer>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                // This creates per-renderer material instances
                Material[] mats = renderers[r].materials;

                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null) continue;

                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", smoothness01);

                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", smoothness01);
                }
            }
        }
    }

    public void ShowGemCountUI(bool _show)
    {
        gemCountUI.SetActive(_show);
    }

    public void SetTargetGem(int _count)
    {
        targetGem[0].sprite = numbers[_count / 10];
        targetGem[1].sprite = numbers[_count % 10];
    }

    public void SetCurrentGem(int _count)
    {
        currentGem[0].sprite = numbers[_count / 10];
        currentGem[1].sprite = numbers[_count % 10];
    }

    public void SetPowerupIcon(PowerupType _powerUp)
    {
        switch (_powerUp)
        {
            case PowerupType.None:
                powerupHUD.texture = powerupIcon[0];
                break;
            case PowerupType.SuperJump:
                powerupHUD.texture = powerupIcon[1];
                break;
            case PowerupType.SuperSpeed:
                powerupHUD.texture = powerupIcon[2];
                break;
            case PowerupType.SuperBounce:
                powerupHUD.texture = powerupIcon[3];
                break;
            case PowerupType.ShockAbsorber:
                powerupHUD.texture = powerupIcon[4];
                break;
            case PowerupType.Gyrocopter:
                powerupHUD.texture = powerupIcon[5];
                break;
            default:
                powerupHUD.texture = powerupIcon[0];
                break;
        }
    }

    public void SetCenterText(string _text)
    {
        centerTextFade?.Kill();

        _text = Utils.Resolve(Regex.Unescape(_text));

        centerText.color = Color.white;
        centerText.text = _text;
        centerTextFade = centerText.DOColor(Color.white, 3f).OnComplete(() => { centerText.DOColor(Color.clear, 0.25f); });
    }

    public void SetBottomText(string _text, float _time = 3f)
    {
        bottomTextFade?.Kill();

        _text = Utils.Resolve(_text).Replace("\\", "");

        bottomText.color = Color.yellow;
        bottomText.text = _text;
        bottomTextFade = bottomText.DOColor(Color.yellow, _time).OnComplete(() => { bottomText.DOColor(Color.clear, 0.25f); });
    }

    public void TeleportFadeOutBottomText()
    {
        if (bottomText.text == "Teleporter has been activated, please wait.")
        {
            bottomTextFade?.Kill();
            bottomTextFade = bottomText.DOColor(Color.clear, 0.25f);
        }
    }

    public void SetTimerColor(bool isRed)
    {
        timerColor = isRed ? numbersRed : numbers;

        if (GameManager.instance.timeTravelActive)
            timerColor = numbersGreen;
    }

    public void SetTimerText(float _timeMs)
    {
        int decaminutes = (int)(_timeMs / (10 * 60 * 1000));
        int remainder = (int)(_timeMs % (10 * 60 * 1000));

        int minutes = remainder / (60 * 1000);
        remainder %= 60 * 1000;

        int decaseconds = remainder / (10 * 1000);
        remainder %= 10 * 1000;

        int seconds = remainder / 1000;
        remainder %= 1000;

        int deciseconds = remainder / 100;
        remainder %= 100;

        int centiseconds = remainder / 10;
        int milliseconds = remainder % 10;

        if (!GameManager.alarmIsPlaying)
        {
            timerColor = numbers;
            if (!GameManager.gameStart || GameManager.gameFinish || GameManager.instance.timeTravelActive)
                timerColor = numbersGreen;
            else if (GameManager.notQualified)
                timerColor = numbersRed;
        }

        timerNumbers[0].sprite = timerColor[decaminutes];
        timerNumbers[1].sprite = timerColor[minutes];
        timerNumbers[2].sprite = timerColor[decaseconds];
        timerNumbers[3].sprite = timerColor[seconds];
        timerNumbers[4].sprite = timerColor[deciseconds];
        timerNumbers[5].sprite = timerColor[centiseconds];
        timerNumbers[6].sprite = timerColor[milliseconds];
        timerNumbers[7].sprite = timerColor[10];
        timerNumbers[8].sprite = timerColor[11];
    }

    public void SetCenterImage(int index)
    {
        readyImage.SetActive(false);
        setImage.SetActive(false);
        goImage.SetActive(false);
        outOfBoundsImage.SetActive(false);

        switch (index)
        {
            case 0: readyImage.SetActive(true); break;
            case 1: setImage.SetActive(true); break;
            case 2: goImage.SetActive(true); break;
            case 3: outOfBoundsImage.SetActive(true); break;
        }
    }

    //Replay
    public void ScrollUp()
    {
        scrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(scrollRect.verticalNormalizedPosition + step);
    }

    public void ScrollDown()
    {
        scrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(scrollRect.verticalNormalizedPosition - step);
    }

    private void OnScrollbarValueChanged(float value)
    {
        // Disable when limits reached
        scrollUpButton.interactable = value < 1f;
        scrollDownButton.interactable = value > 0f;
    }

    private void OnChatMessageReceived(
    string username,
    string message,
    string status)
    {
        string displayName =
            string.IsNullOrEmpty(status)
                ? username
                : $"{username} ({status})";

        AddChatLine(
            $"<color=#9D0000>{displayName}:</color> " +
            $"<color=#000000>{message}</color>");
    }

    private void OnSystemMessageReceived(
    string message)
    {
        AddSystemChatMessage(
            message);
    }

    private void OnRecentMessagesReceived(
    IReadOnlyList<ChatMessage> messages)
    {
        chatLines.Clear();

        foreach (ChatMessage message in messages)
        {
            if (message.IsSystem)
            {
                AddSystemChatMessage(
                    message.Message);
            }
            else
            {
                AddNormalChatMessage(
                    message.Username,
                    message.Message,
                    message.Status);
            }
        }
    }

    private void AddChatLine(string line)
    {
        chatLines.Add(line);

        if (chatLines.Count > MaxChatLines)
            chatLines.RemoveAt(0);

        globalChatText.text =
            string.Join("\n", chatLines);
    }

    private void AddNormalChatMessage(
    string username,
    string message,
    string status)
    {
        string displayName =
            string.IsNullOrEmpty(status)
                ? username
                : $"{username} ({status})";

        AddChatLine(
            $"<color=#9D0000>{displayName}:</color> " +
            $"<color=#000000>{message}</color>");
    }

    private void AddSystemChatMessage(
    string message)
    {
        AddChatLine(
            $"<color=#939612>{message}</color>");
    }

    private void LoadChatHistory()
    {
        chatLines.Clear();

        IReadOnlyList<ChatMessage> messages =
            OnlineManager.Instance.Chat.GetRecentMessages();

        foreach (ChatMessage message in messages)
        {
            if (message.IsSystem)
            {
                AddSystemChatMessage(
                    message.Message);
            }
            else
            {
                AddNormalChatMessage(
                    message.Username,
                    message.Message,
                    message.Status);
            }
        }
    }

    private void OpenChatInput()
    {
        if (chatInputOpen)
            return;

        if (OnlineManager.Instance == null)
            return;

        if (OnlineManager.Instance.Chat == null)
            return;

        if (!OnlineManager.Instance.Chat.IsConnected)
            return;

        if (globalChatInput == null)
        {
            Debug.LogError(
                "GameUIManager: Global Chat Input is not assigned!"
            );

            return;
        }

        chatInputOpen = true;

        globalChatInput.gameObject.SetActive(true);

        globalChatInput.text = string.Empty;

        globalChatInput.ActivateInputField();
        globalChatInput.Select();
    }

    private void SendChatInput()
    {
        if (!chatInputOpen)
            return;

        if (globalChatInput == null)
        {
            Debug.LogError(
                "GameUIManager: Global Chat Input is not assigned!"
            );

            return;
        }

        string message =
            globalChatInput.text.Trim();

        if (!string.IsNullOrEmpty(message))
        {
            if (OnlineManager.Instance != null &&
                OnlineManager.Instance.Chat != null)
            {
                OnlineManager.Instance.Chat
                    .SendChat(message)
                    .Forget();
            }
        }

        CloseChatInput();
    }

    private void CancelChatInput()
    {
        if (!chatInputOpen)
            return;

        CloseChatInput();
    }

    private void CloseChatInput()
    {
        chatInputOpen = false;

        if (globalChatInput == null)
        {
            Debug.LogError(
                "GameUIManager: Global Chat Input is not assigned!"
            );

            return;
        }

        globalChatInput.text = string.Empty;

        globalChatInput.DeactivateInputField();

        globalChatInput.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (OnlineManager.Instance == null ||
            OnlineManager.Instance.Chat == null)
            return;

        OnlineManager.Instance.Chat.MessageReceived -=
            OnChatMessageReceived;

        OnlineManager.Instance.Chat.SystemMessageReceived -=
            OnSystemMessageReceived;

        OnlineManager.Instance.Chat.RecentMessagesReceived -=
            OnRecentMessagesReceived;
    }

    private string GetChatUsername(
        string username)
    {
        if (OnlineManager.Instance == null ||
            OnlineManager.Instance.Chat == null)
        {
            return username;
        }

        IReadOnlyList<OnlinePlayer> players =
            OnlineManager.Instance.Chat.GetOnlinePlayers();

        foreach (OnlinePlayer player in players)
        {
            if (player.Username != username)
                continue;

            if (string.IsNullOrEmpty(player.Status))
                return username;

            return $"{username} ({player.Status})";
        }

        return username;
    }
}
