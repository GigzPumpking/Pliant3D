using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILoadingScreen : MonoBehaviour
{
    public GameObject LoadingScreenObject = default;
    public Image LoadingBarFill = default;

    public Camera mainCamera;

    public float cameraPanSpeed;
    public float cameraStartDist;
    private float initCameraDist = 7.5f;

    private void Awake()
    {
        mainCamera = Camera.main;
        if(mainCamera) initCameraDist = mainCamera.orthographicSize;
        cameraPanSpeed = (cameraStartDist / initCameraDist);
    }

    public void LoadScene(int sceneID, AsyncOperation op = null)
    {
        //StartCoroutine(LoadSceneAsync(sceneID, op));
        StartCoroutine(LoadSceneAsyncCameraPan(sceneID, op));
    }
    
    public void LoadScene(string sceneName, AsyncOperation op = null)
    {
        //StartCoroutine(LoadSceneAsync(sceneName, op));
        StartCoroutine(LoadSceneAsyncCameraPan(sceneName, op));
    }

    private IEnumerator LoadSceneAsyncCameraPan(int sceneID, AsyncOperation op = null)
    {
        Debug.LogError($"Loading thru index {sceneID}");
        op ??= SceneManager.LoadSceneAsync(sceneID);
        mainCamera = Camera.main;
        
        LoadingScreenObject.SetActive(true);
        while (!op.isDone)
        {
            float progressVal = Mathf.Clamp01(op.progress / 0.9f);
            LoadingBarFill.fillAmount = progressVal;
            
            yield return null;
        }
        LoadingScreenObject.SetActive(false);
        
        mainCamera.orthographicSize = cameraStartDist;
        while (mainCamera.orthographicSize > initCameraDist)
        {
            mainCamera.orthographicSize -= cameraPanSpeed * Time.fixedDeltaTime;
            yield return null;
        }
        if(mainCamera.orthographicSize != initCameraDist) mainCamera.orthographicSize = initCameraDist;
    }
    
    private IEnumerator LoadSceneAsyncCameraPan(string sceneName, AsyncOperation op = null)
    {
        Debug.LogError($"Loading thru index {sceneName}");
        op ??= SceneManager.LoadSceneAsync(sceneName);
        
        LoadingScreenObject.SetActive(true);
        while (!op.isDone)
        {
            float progressVal = Mathf.Clamp01(op.progress / 0.9f);
            LoadingBarFill.fillAmount = progressVal;
            
            yield return null;
        }
        LoadingScreenObject.SetActive(false);
        
        mainCamera.orthographicSize = cameraStartDist;
        while (mainCamera.orthographicSize > initCameraDist)
        {
            mainCamera.orthographicSize -= cameraPanSpeed * Time.fixedDeltaTime;
            yield return null;
        }
        if(mainCamera.orthographicSize != initCameraDist) mainCamera.orthographicSize = initCameraDist;
    }

    private IEnumerator LoadSceneAsync(int sceneID, AsyncOperation op)
    {
        op ??= SceneManager.LoadSceneAsync(sceneID);

        LoadingScreenObject.SetActive(true);
        while (!op.isDone)
        {
            float progressVal = Mathf.Clamp01(op.progress / 0.9f);
            LoadingBarFill.fillAmount = progressVal;
            
            yield return null;
        }
        LoadingScreenObject.SetActive(false);
    }
    
    private IEnumerator LoadSceneAsync(string sceneName, AsyncOperation op)
    {
        op ??= SceneManager.LoadSceneAsync(sceneName);

        LoadingScreenObject.SetActive(true);
        while (!op.isDone)
        {
            float progressVal = Mathf.Clamp01(op.progress / 0.9f);
            LoadingBarFill.fillAmount = progressVal;
            
            yield return null;
        }
        LoadingScreenObject.SetActive(false);
    }
}