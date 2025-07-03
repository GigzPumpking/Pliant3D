using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PM_Meditation : MonoBehaviour
{
    [SerializeField] private PM_MeditationData _meditationData;
    private float _meditateRate;
    private float _meditateTime;
    private float _meditateZoomPercent;
    private float _meditateZoom { get => _meditateZoomPercent * Camera.main.orthographicSize; set => _meditateZoomPercent = value; }
    
    void Start()
    {
        _meditateRate = _meditationData.meditateRate;
        _meditateTime = _meditationData.meditateTime;
        _meditateZoomPercent = _meditationData.meditateZoomPercent;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            //if(Camera.main.orthographicSize >= _meditateZoom)
            //Camera.main.orthographicSize +=
        }
    }

    public void Meditate()
    {
        StartCoroutine(MeditateCoroutine());
    }

    private IEnumerator MeditateCoroutine()
    {
        //do animation & UI effects
        
        //replenish lockout
        
        yield return null;
    }
}
