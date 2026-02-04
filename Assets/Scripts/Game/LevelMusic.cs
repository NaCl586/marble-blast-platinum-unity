using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelMusic : MonoBehaviour
{
    public static LevelMusic instance;

    private void Awake()
    {
        instance = this;
    }
    public List<AudioClip> musicsGold;
    public List<AudioClip> musicPlatinum;

    public void Start()
    {
        musicsGold = musicsGold
        .OrderByDescending(clip => clip.name)
        .ToList();

        musicPlatinum = musicPlatinum
        .OrderByDescending(clip => clip.name)
        .ToList();
    }

    public void SetMusic(string musicName, int levelNumber, Game game, Type type)
    {
        if(game == Game.platinum || type == Type.custom)
        {
            AudioClip clip = musicPlatinum.FirstOrDefault(c => c != null && c.name == musicName);
            if(clip)
                GetComponent<AudioSource>().clip = clip;
            else
                GetComponent<AudioSource>().clip = musicPlatinum[(levelNumber - 1) % musicPlatinum.Count];
        }
        else if(game == Game.gold)
        {
            AudioClip clip = musicPlatinum.FirstOrDefault(c => c != null && c.name == musicName);
            if (clip)
                GetComponent<AudioSource>().clip = clip;
            else
                GetComponent<AudioSource>().clip = musicsGold[(levelNumber - 1) % musicsGold.Count];
        }
    }
}
