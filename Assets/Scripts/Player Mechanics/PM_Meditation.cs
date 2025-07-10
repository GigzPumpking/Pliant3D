using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PM_Meditation : MonoBehaviour
{
    [SerializeField] private PM_MeditationData _meditationData; 
    [SerializeField] private GameObject volumePrefab;
    private Volume volumeOverlay;
    private float _meditateRecoverPercent = 100f;
    private float _meditateRate = 2f;
    private float _meditateCameraSizeRatio = 0.95f;
    private float _meditateZoom { get => _originalZoom * _meditateCameraSizeRatio; set => _meditateCameraSizeRatio = value; }
    //the 100f in _meditateAmount is the maxLockoutCharge. Change accordingly
    private float _meditateAmount { get => 100f*(_meditateRecoverPercent/100f); set => _meditateRecoverPercent = value; }
    private float _originalZoom;
    private bool _isMeditating = false;
    private TransformationWheel _transformationWheel;
    
    void Start()
    {
        _originalZoom = Camera.main.orthographicSize;
        _meditateRecoverPercent = _meditationData.meditateRecoverPercent;
        _meditateRate = _meditationData.meditateRate;
        _meditateCameraSizeRatio = _meditationData.meditateCameraSizeRatio;
        volumeOverlay = GameObject.Instantiate(volumePrefab).GetComponent<Volume>();
        _transformationWheel = Player.Instance.transformationWheelScript.GetComponent<TransformationWheel>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !_isMeditating)
        {
            Meditate();
        }
    }

    public void Meditate()
    {
        _isMeditating = true;
        StartCoroutine(MeditateCoroutine());
    }

    private IEnumerator MeditateCoroutine()
    {
        //camera zoom & vignette
        while (Camera.main.orthographicSize >= _meditateZoom)
        {
            DoFancy();
            yield return null;
        }
        
        bool op = false;
        while (!op)
        {
            _transformationWheel.AddProgressToAllForms(Time.deltaTime * _meditateRate);
            op = Player.Instance.transformationWheelScript.LockoutProgresses[Transformation.FROG] >= _meditateAmount && 
                 Player.Instance.transformationWheelScript.LockoutProgresses[Transformation.BULLDOZER] >= _meditateAmount;
            yield return null;
        }
        
        //reverse camera zoom & vignette
        while (Camera.main.orthographicSize <= _originalZoom)
        {
            UndoFancy();
            yield return null;
        }
        
        _isMeditating = false;
    }

    private void UndoFancy()
    {
        if (volumeOverlay)
        {
            Vignette vignette = volumeOverlay.profile.components[0] as Vignette;
            vignette.intensity.value -= Time.deltaTime;
            vignette.center = new Vector2Parameter(new Vector2(Player.Instance.transform.position.x,
                Player.Instance.transform.position.y));
            
            if(vignette.intensity.value <= 0) volumeOverlay.enabled = false;
        }

        Camera.main.orthographicSize += Time.deltaTime; 
    }

    private void DoFancy()
    {
        if (volumeOverlay) volumeOverlay.enabled = true;
        
        Vignette vignette = volumeOverlay.profile.components[0] as Vignette;
        vignette.center = new Vector2Parameter(new Vector2(Player.Instance.transform.position.x, Player.Instance.transform.position.y));
        vignette.intensity.value += Time.deltaTime;

        Camera.main.orthographicSize -= Time.deltaTime;
    }
}
