using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.Video;

public class TransitionMovies : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    public AudioClip audioClip;
    public bool playOnStart = true;
    
    // Start is called before the first frame update
    void Start()
    {
        gameObject.TryGetComponent(out videoPlayer);
        if(playOnStart) PlayVideoCoroutinePublic();
    }

    // Update is called once per frame
    void Update()
    {
        //literally awful but it works and I don't have time to make it better rn
        gameObject.transform.SetAsLastSibling();
    }
    public IEnumerator PlayVideoCoroutine()
    {
        yield return new WaitForSeconds((float)videoPlayer.clip.length);
        Debug.LogWarning("Playing video complete");
        AudioManager.Instance?.PlayMainTheme();
        gameObject.SetActive(false);
    }
    
    //For calls in main menu 
    public void PlayVideoCoroutinePublic()
    {
        if (!videoPlayer || videoPlayer.isPlaying)
        {
            Debug.Log("Video player is not ready or already playing.");
            return;
        }
        videoPlayer.Play();
        AudioSource src;
        TryGetComponent(out src);
        if (audioClip != null && src)
        {
            src.clip = audioClip;
            src.Play();
        } 
        StartCoroutine(PlayVideoCoroutine());
    }
}
