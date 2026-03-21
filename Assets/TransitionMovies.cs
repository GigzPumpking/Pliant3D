using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TransitionMovies : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public bool playOnStart = true;
    
    // Start is called before the first frame update
    void Start()
    {
        if(playOnStart) Invoke(nameof(PlayVideoCoroutine), 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator PlayVideoCoroutine()
    {
        videoPlayer.Play();
        while (videoPlayer.isPlaying)
        {
            yield return null;
        }
        AudioManager.Instance?.PlayDefaultTrack();
    }
    
    //For calls in main menu 
    public void PlayVideoCoroutinePublic()
    {
        StartCoroutine(PlayVideoCoroutine());
    }
}
