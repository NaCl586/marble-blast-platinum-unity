using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Radar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas canvas;

    [Header("Radar Icons")]
    [Tooltip("Must match the order of Gem.gemColors.")]
    [SerializeField] private Sprite[] gemRadarIcons;

    [SerializeField] private Sprite endPadIcon;
    [SerializeField] private Sprite pointerIcon;

    [Header("Radar Position")]
    [SerializeField]
    private Vector2 ellipseScreenFraction =
        new Vector2(0.79f, 0.85f);

    [Header("Icon Size")]
    [SerializeField]
    private Vector2 gemIconSize =
        new Vector2(32f, 32f);

    [SerializeField]
    private Vector2 endPadIconSize =
        new Vector2(32f, 32f);

    [Header("Pointer")]
    [SerializeField]
    private Vector2 pointerSize =
        new Vector2(100f, 70f);

    [SerializeField, Range(0f, 1f)]
    private float pointerAlpha = 0.6f;

    private class GemMarker
    {
        public Gem gem;
        public Image icon;
        public Image pointer;
    }

    private readonly List<GemMarker> gemMarkers =
        new List<GemMarker>();

    private Image endPadIconImage;
    private Image endPadPointerImage;

    private RectTransform canvasRect;

    private bool initialized;
    private bool showingEndPad;

    private const string RadarVisibleKey = "RadarVisible";
    private bool radarVisible = true;

    private void Awake()
    {
        if (canvas != null)
        {
            canvasRect =
                canvas.GetComponent<RectTransform>();
        }

        radarVisible =
            PlayerPrefs.GetInt(
                RadarVisibleKey,
                1
            ) == 1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            radarVisible = !radarVisible;

            PlayerPrefs.SetInt(
                RadarVisibleKey,
                radarVisible ? 1 : 0
            );

            PlayerPrefs.Save();

            if (!radarVisible)
            {
                HideEverything();
            }
        }
    }

    private void LateUpdate()
    {
        if (!radarVisible)
            return;

        if (GameManager.instance == null)
            return;

        if (playerCamera == null ||
            canvas == null)
            return;

        /*
         * GameManager initializes its gem array during
         * its own initialization. Wait until it exists
         * before creating our radar markers.
         */
        if (!initialized)
        {
            if (GameManager.instance.Gems == null)
                return;

            InitializeGemMarkers();
            InitializeEndPadMarkers();

            initialized = true;
        }

        if (GameManager.gameFinish)
        {
            HideEverything();
            return;
        }

        /*
         * Don't call CheckForAllGems() every frame.
         * GameManager already tracks these values.
         */
        bool allGemsCollected = GameManager.instance.CheckForAllGems();

        if (allGemsCollected)
        {
            UpdateEndPad();
        }
        else
        {
            UpdateGems();
        }
    }

    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void InitializeGemMarkers()
    {
        Gem[] gems =
            GameManager.instance.Gems;

        if (gems == null)
            return;

        foreach (Gem gem in gems)
        {
            if (gem == null)
                continue;

            GemMarker marker =
                new GemMarker();

            marker.gem = gem;

            marker.icon =
                CreateImage(
                    "Radar Gem Icon"
                );

            marker.pointer =
                CreateImage(
                    "Radar Gem Pointer"
                );

            marker.pointer.preserveAspect =
                true;

            marker.icon.gameObject.SetActive(
                false
            );

            marker.pointer.gameObject.SetActive(
                false
            );

            gemMarkers.Add(marker);
        }
    }

    private void InitializeEndPadMarkers()
    {
        endPadIconImage =
            CreateImage(
                "Radar End Pad Icon"
            );

        endPadPointerImage =
            CreateImage(
                "Radar End Pad Pointer"
            );

        endPadPointerImage.preserveAspect =
            true;

        endPadIconImage.gameObject.SetActive(
            false
        );

        endPadPointerImage.gameObject.SetActive(
            false
        );
    }

    private Image CreateImage(string objectName)
    {
        GameObject obj =
            new GameObject(objectName);

        obj.transform.SetParent(
            transform,
            false
        );

        Image image =
            obj.AddComponent<Image>();

        image.raycastTarget =
            false;

        return image;
    }

    // ==================================================
    // GEM RADAR
    // ==================================================

    private void UpdateGems()
    {
        if (showingEndPad)
        {
            HideEndPad();

            showingEndPad = false;
        }

        foreach (GemMarker marker in gemMarkers)
        {
            Gem gem =
                marker.gem;

            if (gem == null)
            {
                HideMarker(marker);
                continue;
            }

            /*
             * PickupItem() disables the entire Gem.
             */
            if (!gem.gameObject.activeInHierarchy)
            {
                HideMarker(marker);
                continue;
            }

            int index =
                gem.gemColorIndex;

            if (index < 0 ||
                index >= gemRadarIcons.Length)
            {
                HideMarker(marker);
                continue;
            }

            Sprite gemIcon =
                gemRadarIcons[index];

            if (gemIcon == null)
            {
                HideMarker(marker);
                continue;
            }

            /*
             * The pointer color comes directly from
             * the Gem script.
             */
            Color pointerColor =
                gem.gemColor;

            UpdateTarget(
                GetGemWorldPosition(gem),
                gemIcon,
                pointerColor,
                marker.icon,
                marker.pointer,
                gemIconSize
            );
        }
    }

    private Vector3 GetGemWorldPosition(
        Gem gem)
    {
        Collider collider =
            gem.GetComponent<Collider>();

        if (collider != null)
            return collider.bounds.center;

        return gem.transform.position;
    }

    // ==================================================
    // END PAD RADAR
    // ==================================================

    private void UpdateEndPad()
    {
        if (!showingEndPad)
        {
            HideAllGemMarkers();

            showingEndPad = true;
        }

        GameObject finishPad =
            GameManager.instance.finishPad;

        if (finishPad == null)
        {
            HideEndPad();
            return;
        }

        /*
         * Original Haxe radar uses:
         *
         * 0xE6E6E6
         */
        Color endPadPointerColor =
            new Color32(
                0xE6,
                0xE6,
                0xE6,
                0xFF
            );

        UpdateTarget(
            finishPad.transform.position,
            endPadIcon,
            endPadPointerColor,
            endPadIconImage,
            endPadPointerImage,
            endPadIconSize
        );
    }

    // ==================================================
    // TARGET
    // ==================================================

    private void UpdateTarget(
        Vector3 worldPosition,
        Sprite icon,
        Color pointerColor,
        Image iconImage,
        Image pointerImage,
        Vector2 iconSize)
    {
        if (icon == null)
        {
            iconImage.gameObject.SetActive(
                false
            );

            pointerImage.gameObject.SetActive(
                false
            );

            return;
        }

        /*
         * WorldToScreenPoint gives us the actual
         * screen position.
         */
        Vector3 screenPosition =
            playerCamera.WorldToScreenPoint(
                worldPosition
            );

        /*
         * WorldToViewportPoint gives us a cheap
         * on-screen/off-screen test.
         */
        Vector3 viewport =
            playerCamera.WorldToViewportPoint(
                worldPosition
            );

        bool visible =
            viewport.z > 0f &&
            viewport.x >= 0f &&
            viewport.x <= 1f &&
            viewport.y >= 0f &&
            viewport.y <= 1f;

        if (visible)
        {
            ShowIcon(
                iconImage,
                icon,
                screenPosition,
                iconSize
            );

            pointerImage.gameObject.SetActive(
                false
            );
        }
        else
        {
            iconImage.gameObject.SetActive(
                false
            );

            ShowPointer(
                pointerImage,
                screenPosition,
                pointerColor
            );
        }
    }

    // ==================================================
    // ICON
    // ==================================================

    private void ShowIcon(
        Image image,
        Sprite sprite,
        Vector3 screenPosition,
        Vector2 size)
    {
        image.sprite =
            sprite;

        image.color =
            Color.white;

        image.rectTransform.sizeDelta =
            size;

        SetUIPosition(
            image.rectTransform,
            screenPosition
        );

        image.rectTransform.rotation =
            Quaternion.identity;

        image.gameObject.SetActive(
            true
        );
    }

    // ==================================================
    // POINTER
    // ==================================================

    private void ShowPointer(
        Image pointer,
        Vector3 screenPosition,
        Color pointerColor)
    {
        Vector2 screenSize =
            new Vector2(
                Screen.width,
                Screen.height
            );

        Vector2 screenCenter =
            screenSize * 0.5f;

        Vector2 projectedPosition =
            new Vector2(
                screenPosition.x,
                screenPosition.y
            );

        Vector2 direction =
            projectedPosition -
            screenCenter;

        bool behindCamera =
            screenPosition.z < 0f;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector2.up;
        }
        else
        {
            direction.Normalize();
        }

        if (behindCamera)
            direction *= -1f;

        float theta =
            Mathf.Atan2(
                direction.y,
                direction.x
            );

        /*
         * Same ellipse calculation as the Haxe radar.
         */
        Vector2 ellipsePosition =
            new Vector2(
                screenSize.x *
                (
                    ellipseScreenFraction.x *
                    Mathf.Cos(theta) +
                    1f
                ) / 2f,

                screenSize.y *
                (
                    ellipseScreenFraction.y *
                    Mathf.Sin(theta) +
                    1f
                ) / 2f
            );

        /*
         * Pointer.png is assumed to point right
         * at 0 degrees.
         */
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        pointer.sprite =
            pointerIcon;

        pointer.color =
            new Color(
                pointerColor.r,
                pointerColor.g,
                pointerColor.b,
                pointerAlpha
            );

        pointer.rectTransform.sizeDelta =
            pointerSize;

        SetUIPosition(
            pointer.rectTransform,
            ellipsePosition
        );

        pointer.rectTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        pointer.gameObject.SetActive(
            true
        );
    }

    // ==================================================
    // UI POSITION
    // ==================================================

    private void SetUIPosition(
        RectTransform rect,
        Vector3 screenPosition)
    {
        Camera uiCamera =
            null;

        if (canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                canvas.worldCamera;
        }

        Vector2 localPoint;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out localPoint))
        {
            rect.anchoredPosition =
                localPoint;
        }
    }

    // ==================================================
    // HIDING
    // ==================================================

    private void HideMarker(
        GemMarker marker)
    {
        marker.icon.gameObject.SetActive(
            false
        );

        marker.pointer.gameObject.SetActive(
            false
        );
    }

    private void HideAllGemMarkers()
    {
        foreach (GemMarker marker in gemMarkers)
        {
            HideMarker(marker);
        }
    }

    private void HideEndPad()
    {
        if (endPadIconImage != null)
        {
            endPadIconImage.gameObject.SetActive(
                false
            );
        }

        if (endPadPointerImage != null)
        {
            endPadPointerImage.gameObject.SetActive(
                false
            );
        }
    }

    private void HideEverything()
    {
        HideAllGemMarkers();
        HideEndPad();
    }

    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        foreach (GemMarker marker in gemMarkers)
        {
            if (marker.icon != null)
            {
                Destroy(
                    marker.icon.gameObject
                );
            }

            if (marker.pointer != null)
            {
                Destroy(
                    marker.pointer.gameObject
                );
            }
        }

        gemMarkers.Clear();

        if (endPadIconImage != null)
        {
            Destroy(
                endPadIconImage.gameObject
            );
        }

        if (endPadPointerImage != null)
        {
            Destroy(
                endPadPointerImage.gameObject
            );
        }
    }
}