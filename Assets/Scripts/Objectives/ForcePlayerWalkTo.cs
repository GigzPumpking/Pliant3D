using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ForcePlayerWalkTo : MonoBehaviour
{
    [SerializeField] List<UnityEvent> events = new List<UnityEvent>();
    [SerializeField] private GameObject destination = null;
    private bool _inProgress = false;
    private CancellationTokenSource _cancellationTokenSource;
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        MovePlayerTo(destination);
    }

    private async void MovePlayerTo(GameObject dest)
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;
            await MyAsyncTask(token, dest);
        }
        catch(Exception e) { 
            Debug.Log(e);
        }
    }

    private void StopTask()
    {
        if (_cancellationTokenSource == null) return;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    private Task MyAsyncTask(CancellationToken token, GameObject dest)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                
            }
            catch (TaskCanceledException)
            {
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    void OnDestroy()
    {
        StopTask(); // Ensure cancellation on object destruction
    }
    
    
}
