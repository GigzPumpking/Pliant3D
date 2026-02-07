using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionHandler : MonoBehaviour
{
    public void NextScene()
    {
        SceneLoader.Instance.LoadNextScene();
    }
}
