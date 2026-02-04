using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    public static MenuMusic instance;
    public AudioClip pianoforte;
    public AudioClip quietLab;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        GetComponent<AudioSource>().volume = PlayerPrefs.GetFloat("Audio_MusicVolume", 0.5f);
    }

    public void PlayPianoforte()
    {
        GetComponent<AudioSource>().Stop();
        GetComponent<AudioSource>().clip = pianoforte;
        GetComponent<AudioSource>().Play();
    }

    public void PlayQuietLab()
    {
        GetComponent<AudioSource>().Stop();
        GetComponent<AudioSource>().clip = quietLab;
        GetComponent<AudioSource>().Play();
    }
}
