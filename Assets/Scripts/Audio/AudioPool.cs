using UnityEngine;
using System.Collections.Generic;

public class AudioPool : MonoBehaviour
{
    public static AudioPool Instance { get; private set; }

    [SerializeField] private GameObject audioSourcePrefab; // Prefab with an AudioSource component
    [SerializeField] private int poolSize = 10; // Number of AudioSources in the pool
    
    private Queue<AudioSource> audioPool = new Queue<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(audioSourcePrefab, transform);
            AudioSource source = obj.GetComponent<AudioSource>();
            obj.SetActive(false);
            audioPool.Enqueue(source);
        }
    }

    public AudioSource GetAudioSource(Transform parent)
    {
        if (audioPool.Count > 0)
        {
            AudioSource source = audioPool.Dequeue();
            source.gameObject.SetActive(true);
            if (parent != null)
            {
                source.transform.SetParent(parent);
                source.transform.localPosition = Vector3.zero;
            }
            return source;
        }
        else
        {
            Debug.LogWarning("Audio Pool Exhausted! Consider increasing the pool size.");
            return null;
        }
    }

    public void ReturnAudioSource(AudioSource source)
    {
        source.Stop();
        source.transform.SetParent(transform); // Reset parent to pool manager
        source.gameObject.SetActive(false);
        audioPool.Enqueue(source);
    }
}
