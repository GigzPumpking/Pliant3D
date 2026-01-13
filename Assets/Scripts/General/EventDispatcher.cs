using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Event 
{

}

public class PlaySound : Event
{
    public string soundName;
    public AudioSource source;
}

public class ReachedTarget : Event
{
    public GameObject obj;
}

public class PlayMusic : Event
{
    public string musicName;
}

public class StopMusic : Event
{
    public string musicName;
}

public class Interact : Event
{
    public DialogueTrigger questGiver;
}

public class ObjectiveInteractEvent : Event
{
    public GameObject interactedTo;
    public Transformation currentTransformation;
}

public class ObjectiveInteracted : Event
{
    public GameObject interactedTo;
}

public class EndDialogue : Event
{
    public string someEntry;
    public EndDialogue(){ }
    public EndDialogue(string data) => someEntry = data;
}

public class PlayGame : Event
{

}

public class QuitGame : Event
{

}

public class PressButton : Event
{

}

public class ReleaseButton : Event
{

}

public class StressDebuff : Event
{

}

public class StressAbility : Event {}

public class Heal : Event {}
public class ShiftAbility : Event
{
    public bool isEnabled = false;

    public Transformation transformation = Transformation.TERRY;
}

public class NewSceneLoaded : Event
{
    public string sceneName;
}

public class TogglePlayerMovement : Event
{
    public bool isEnabled = false;
}

public class DebugMessage : Event
{
    public string message;
}

public class EventDispatcher 
{
    private static EventDispatcher _instance;

    private EventDispatcher()
    {
    }

    public static EventDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EventDispatcher();
            }
            return _instance;
        }
    }

    public delegate void EventDelegate<T>(T e) where T : Event;

    private Dictionary<System.Type, System.Delegate> m_eventDelegates = new Dictionary<System.Type, System.Delegate>();

    private void _AddListener<T>(EventDelegate<T> listener) where T : Event
    {
        System.Type eventType = typeof(T);
        System.Delegate del;

        if (m_eventDelegates.TryGetValue(typeof(T), out del))
        {
            del = System.Delegate.Combine(del, listener);
            // if m_eventDelegates already contains the key, remove it first
            if (m_eventDelegates.ContainsKey(typeof(T))) m_eventDelegates.Remove(typeof(T));
            m_eventDelegates.Add(typeof(T), del);
        }
        else
        {
            m_eventDelegates.Add(typeof(T), listener);
        }
    }

    public static void AddListener<T>(EventDelegate<T> listener) where T : Event
    {
        Instance._AddListener(listener);
    }

    private void _RemoveListener<T>(EventDelegate<T> listener) where T : Event
    {
        //Remove a listener from an event
        System.Type eventType = typeof(T);
        System.Delegate del;

        if (m_eventDelegates.TryGetValue(typeof(T), out del))
        {
            System.Delegate newDel = System.Delegate.Remove(del, listener);

            if (newDel == null)
            {
                m_eventDelegates.Remove(typeof(T));
            }
            else
            {
                m_eventDelegates[typeof(T)] = newDel;
            }
        }
    }

    public static void RemoveListener<T>(EventDelegate<T> listener) where T : Event
    {
        Instance._RemoveListener(listener);
    }

    private void _Raise<T>(T e) where T : Event
    {
        //Raise an event
        System.Delegate del;
        if (m_eventDelegates.TryGetValue(typeof(T), out del))
        {
            EventDelegate<T> callback = del as EventDelegate<T>;
            if (callback != null)
            {
                callback(e);
            }
        }
    }

    public static void Raise<T>(T e) where T : Event
    {
        Instance._Raise(e);
    }
}
