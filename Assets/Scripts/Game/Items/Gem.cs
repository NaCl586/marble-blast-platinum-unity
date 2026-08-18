using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField] Texture[] gemColors;
    [SerializeField] SkinnedMeshRenderer smr;
    [SerializeField] AudioClip pickupSound;
    [SerializeField] AudioClip pickupLastGem;

    public Color gemColor;

    // The index of the texture selected for this gem.
    // The radar uses this to select the corresponding GemItem*.png.
    public int gemColorIndex { get; private set; }

    void Start()
    {
        gemColorIndex = Random.Range(0, gemColors.Length);

        smr.materials[0].mainTexture =
            gemColors[gemColorIndex];

        // Keep the existing gem color behavior.
        Texture2D tex2D =
            gemColors[gemColorIndex] as Texture2D;

        if (tex2D != null)
        {
            gemColor = tex2D.GetPixel(0, 0);
            gemColor.a = 1f;
        }
        else
        {
            gemColor = Color.white;
        }
    }

    private void FixedUpdate()
    {
        var rot = transform.Find("Mesh").rotation;

        transform.Find("Mesh").rotation =
            Quaternion.AngleAxis(
                Time.fixedDeltaTime * 120f,
                rot * Vector3.up
            ) * rot;
    }

    public void PickupItem()
    {
        GameManager.onCollectGem?.Invoke(GameManager.instance.currentGems + 1);

        if (GameManager.instance.CheckForAllGems())
            GameManager.instance.PlayAudioClip(pickupLastGem);
        else
            GameManager.instance.PlayAudioClip(pickupSound);

        GameManager.instance.recentGems.Add(gameObject);
        gameObject.SetActive(false);
    }
}