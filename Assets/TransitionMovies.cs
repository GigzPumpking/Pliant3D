using System;
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
    public bool playOnce = true;
    
    // Start is called before the first frame update
    void Start()
    {
        gameObject.TryGetComponent(out videoPlayer);
        if(playOnStart && (playOnce && !GameManager.Instance.VideoHasPlayed)) PlayVideoCoroutinePublic();
    }

    // Update is called once per frame
    void Update()
    {
        //literally awful but it works and I don't have time to make it better rn
        if(gameObject.activeInHierarchy) gameObject.transform.SetAsLastSibling();
    }
    private IEnumerator PlayVideoCoroutine()
    {
        //if(videoPlayer)
        GameManager.Instance.VideoHasPlayed = true;
        Debug.LogWarning("Video length : " + videoLength);
        videoPlayer.Play();
        
        //yield return new WaitWhile(videoPlayer.clip ? () => videoPlayer.isPlaying : () => Time.time < videoLength);
        yield return new WaitWhile(() => videoPlayer.isPlaying);
        Debug.LogWarning("Playing video complete");
        gameObject.SetActive(false);
        AudioManager.Instance?.PlayMainTheme();
        yield return null;
    }

    private float videoLength;
    //For calls in main menu 
    public void PlayVideoCoroutinePublic()
    {
        if (!videoPlayer || videoPlayer.isPlaying || videoPlayer.isPaused)
        {
            Debug.Log("Video player is not ready or already playing.");
            videoPlayer.gameObject.SetActive(false);
            videoLength = 0f;
            return;
        }
        
        videoLength = (float)videoPlayer.clip.length;
        
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
