using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PM_Meditation : MonoBehaviour
{
    [SerializeField] private PM_MeditationData _mData; 
    [SerializeField] private GameObject volumePrefab;
    private Volume _volumeOverlay;
    private TransformationWheel _transformationWheel;
    
    private float _meditateZoom { get => _originalZoom * _mData.meditateCameraSizeRatio; }
    private float _meditateAmount { get => 100*(_mData.meditateRecoverPercent/100f); } //the 100f in _meditateAmount is the maxLockoutCharge. Change accordingly
    private float _originalZoom = 7.5f;
    
    private bool _isMeditating = false;
    private Func<IEnumerator> meditateCo;
    
    void Start()
    {
        _originalZoom = Camera.main.orthographicSize;
        _volumeOverlay = GameObject.Instantiate(volumePrefab).GetComponent<Volume>();
        _transformationWheel = Player.Instance.transformationWheelScript.GetComponent<TransformationWheel>();
        meditateCo = MeditateCoroutine;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !_isMeditating) Meditate();
    }

    public void Meditate()
    {
        if (!_transformationWheel.isLockedOut && _mData.onlyMeditateOnLockout) return;
        
        _isMeditating = true;
        //StartCoroutine(MeditateCoroutine());
        StartCoroutine(meditateCo());
    }

    private IEnumerator MeditateCoroutine()
    {
        Debug.LogError("Starting Meditation");
        while(!DoFancy()) yield return null;
        
        bool op = false;
        while (!op)
        {
            Debug.LogError("Performing Meditation");
            _transformationWheel.AddProgressToAllForms(Time.deltaTime * _mData.meditateRate);
            op = Player.Instance.transformationWheelScript.LockoutProgresses[Transformation.FROG] >= _meditateAmount && 
                 Player.Instance.transformationWheelScript.LockoutProgresses[Transformation.BULLDOZER] >= _meditateAmount;
            yield return null;
        }
        
        Debug.LogError("Done with Meditation");
        while(!UndoFancy()) yield return null;
        
        _isMeditating = false;
        yield return null;
    }

    private bool UndoFancy()
    {
        if (Camera.main.orthographicSize <= _originalZoom)
        {
            if (_volumeOverlay)
            {
                Vignette vignette = _volumeOverlay.profile.components[0] as Vignette;
                vignette.intensity.value -= Time.deltaTime;
                vignette.center = new Vector2Parameter(new Vector2(Player.Instance.transform.position.x,
                    Player.Instance.transform.position.y));

                if (vignette.intensity.value <= 0) _volumeOverlay.enabled = false;
            }

            Camera.main.orthographicSize += Time.deltaTime;
            return false;
        }

        return true;
    }

    private bool DoFancy()
    {
        if (Camera.main.orthographicSize >= _meditateZoom)
        {
            if (_volumeOverlay) _volumeOverlay.enabled = true;

            Vignette vignette = _volumeOverlay.profile.components[0] as Vignette;
            vignette.center = new Vector2Parameter(new Vector2(Player.Instance.transform.position.x,
                Player.Instance.transform.position.y));
            vignette.intensity.value += Time.deltaTime;

            Camera.main.orthographicSize -= Time.deltaTime;
            return false;
        }

        return true;
    }
}
