using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTextAutoSize : MonoBehaviour
{
    [SerializeField] private float padding = 4f;

    private TextMeshProUGUI text;
    private RectTransform rect;

    private float lastHeight = -1;

    void OnEnable()
    {
        text = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (text == null)
            return;

        float preferredHeight = text.preferredHeight + padding;

        if (!Mathf.Approximately(preferredHeight, lastHeight))
        {
            lastHeight = preferredHeight;

            rect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                preferredHeight);

            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate();
        }
    }
}