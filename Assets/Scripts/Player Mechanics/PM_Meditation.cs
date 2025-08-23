using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class PM_Meditation : KeyActionReceiver<PM_Meditation>
{
    [FormerlySerializedAs("_mData")] [SerializeField] private PM_MeditationData mData;
    [SerializeField] private GameObject volumePrefab;
    [SerializeField] private GameObject meditationOverlay;
    [SerializeField] private TextMeshProUGUI meditationCountDown;
    private Volume _volumeOverlay;
    public static event Action<Transformation> OnMeditate; //Listened to by LockoutBar.cs

    private float meditateZoom
    {
        get => _originalZoom * mData.meditateCameraSizeRatio;
    }

    private float _originalZoom = 7.5f;
    private bool _isMeditating = false;
    private Func<IEnumerator> _meditateCo;

    public static Dictionary<string, Action<PM_Meditation, InputAction.CallbackContext>> staticKeyMapping =
        new Dictionary<string, Action<PM_Meditation, InputAction.CallbackContext>>()
        {
            { "Meditate", (w, ctx) => w.MeditateButton(ctx) }
        };

    protected override Dictionary<string, Action<PM_Meditation, InputAction.CallbackContext>> KeyMapping => staticKeyMapping;

    void Start()
    {
        _originalZoom = Camera.main.orthographicSize;
        _volumeOverlay = GameObject.Instantiate(volumePrefab).GetComponent<Volume>();
        _meditateCo = MeditateCoroutine;
    }

    float currTime = 0f;
    void FixedUpdate()
    {
        //if ((Input.GetKeyDown(KeyCode.Z))) Meditate();
        if((Gamepad.current != null && Gamepad.current.bButton.isPressed) || Input.GetKeyDown(KeyCode.Z)) Meditate();
        
        //if(_isMeditating && Input.anyKeyDown) StopCoroutine(_meditateCo());
        if (_isMeditating)
        {
            if (currTime <= mData.timeForMeditate)
            {
                Debug.Log($"currTime: {currTime}");
                currTime += Time.fixedDeltaTime;
                meditationCountDown.text = ((int)(mData.timeForMeditate - currTime)).ToString();
                op = currTime <= mData.timeForMeditate;
            }
        }
    }
    
    private void MeditateButton(InputAction.CallbackContext ctx)
    {
        Meditate();
    }
    
    public void Meditate()
    {
        if (mData.onlyMeditateOnLockout && !LockoutBar.Instance.IsAnyLockedOut()) return;
        if (Player.Instance?.GetTransformation() != Transformation.TERRY) return;
        if (_isMeditating) return;
        
        StartCoroutine(_meditateCo());
    }

    private bool op;
    private IEnumerator MeditateCoroutine()
    {
        currTime = 0f;
        _isMeditating = true;
        while(!DoFancy()) yield return null;
        meditationOverlay?.SetActive(true);
        Player.Instance?.canMoveToggle(false);
        
        while (currTime <= mData.timeForMeditate) { Debug.Log("Performing"); yield return null;}
        
        OnMeditate?.Invoke(Transformation.FROG);
        OnMeditate?.Invoke(Transformation.BULLDOZER);
        OnMeditate?.Invoke(Transformation.TERRY);
        meditationOverlay?.SetActive(false);
        Player.Instance?.canMoveToggle(true);
        
        //Debug.LogError("Done with Meditation");
        while(!UndoFancy()) yield return null;
        
        _isMeditating = false;
        yield return null;
    }

    private bool UndoFancy()
    {
        if (Camera.main.orthographicSize <= _originalZoom)
        {
            if (!_volumeOverlay) _volumeOverlay = GameObject.Instantiate(volumePrefab).GetComponent<Volume>();
            _volumeOverlay.enabled = false;
            
            Vignette vignette = _volumeOverlay.profile.components[0] as Vignette;
            vignette.intensity.value -= Time.deltaTime;
            vignette.center = new Vector2Parameter(new Vector2(Player.Instance.transform.position.x,
            Player.Instance.transform.position.y));

            if (vignette.intensity.value <= 0) _volumeOverlay.enabled = false;
            
            Camera.main.orthographicSize += Time.deltaTime;
            return false;
        }

        return true;
    }

    private bool DoFancy()
    {
        if (Camera.main.orthographicSize >= meditateZoom)
        {
            if (!_volumeOverlay) _volumeOverlay = GameObject.Instantiate(volumePrefab).GetComponent<Volume>();
            if(_volumeOverlay) _volumeOverlay.enabled = true;
            
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
