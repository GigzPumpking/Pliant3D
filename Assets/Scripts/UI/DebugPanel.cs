using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugPanel : MonoBehaviour
{
    private TextMeshProUGUI debugText;

    [SerializeField] private bool _debug = false;

    void Awake()
    {
        debugText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (!_debug)
        {
            gameObject.SetActive(false);
            return;
        }

        EventDispatcher.AddListener<DebugMessage>(OnDebugMessage);
    }

    private void OnDisable()
    {
        if (!_debug)
        {
            return;
        }

        EventDispatcher.RemoveListener<DebugMessage>(OnDebugMessage);
    }

    private void OnDebugMessage(DebugMessage message)
    {
        debugText.text += message.message + "\n";
    }

}
