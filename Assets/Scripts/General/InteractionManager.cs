using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages proximity-based interactions. Tracks all interactable objects and determines
/// which one the player can interact with based on distance.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    private static InteractionManager instance;
    public static InteractionManager Instance => instance;
    
    [Header("Settings")]
    [Tooltip("Default interaction distance if not specified on the interactable")]
    [SerializeField] private float defaultInteractionDistance = 3f;
    
    [Tooltip("How often to update the closest interactable (in seconds). Lower = more responsive but more CPU.")]
    [SerializeField] private float updateInterval = 0.1f;
    
    // All registered interactables
    private HashSet<IInteractable> registeredInteractables = new HashSet<IInteractable>();
    
    // The currently closest interactable that the player can interact with
    private IInteractable currentClosestInteractable;
    public IInteractable CurrentClosestInteractable => currentClosestInteractable;
    
    private float updateTimer = 0f;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void OnEnable()
    {
        EventDispatcher.AddListener<Interact>(OnInteract);
    }
    
    private void OnDisable()
    {
        EventDispatcher.RemoveListener<Interact>(OnInteract);
    }
    
    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateClosestInteractable();
        }
    }
    
    /// <summary>
    /// Register an interactable to be tracked by the manager.
    /// </summary>
    public void Register(IInteractable interactable)
    {
        registeredInteractables.Add(interactable);
    }
    
    /// <summary>
    /// Unregister an interactable from the manager.
    /// </summary>
    public void Unregister(IInteractable interactable)
    {
        registeredInteractables.Remove(interactable);
        
        // If this was the current closest, clear it
        if (currentClosestInteractable == interactable)
        {
            currentClosestInteractable?.SetInteractBubbleActive(false);
            currentClosestInteractable = null;
        }
    }
    
    /// <summary>
    /// Finds the closest interactable within range and updates the interact bubble.
    /// Optimized to use squared distance and early distance rejection.
    /// </summary>
    private void UpdateClosestInteractable()
    {
        if (Player.Instance == null) return;
        
        Vector3 playerPos = Player.Instance.transform.position;
        
        IInteractable closest = null;
        float closestDistanceSqr = float.MaxValue;
        
        foreach (var interactable in registeredInteractables)
        {
            if (interactable == null) continue;
            
            Vector3 interactablePos = interactable.GetPosition();
            float maxDistance = interactable.GetInteractionDistance();
            
            // Early rejection: Use squared distance to avoid expensive sqrt
            float distanceSqr = (playerPos - interactablePos).sqrMagnitude;
            float maxDistanceSqr = maxDistance * maxDistance;
            
            // Skip if too far away - don't even check IsInteractable()
            if (distanceSqr > maxDistanceSqr) continue;
            
            // Only check interactability if within range
            if (!interactable.IsInteractable()) continue;
            
            // Check if this is closer than current closest
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = interactable;
            }
        }
        
        // Update interact bubbles if the closest changed
        if (closest != currentClosestInteractable)
        {
            // Hide old bubble
            currentClosestInteractable?.SetInteractBubbleActive(false);
            
            // Show new bubble
            currentClosestInteractable = closest;
            currentClosestInteractable?.SetInteractBubbleActive(true);
        }
    }
    
    /// <summary>
    /// Called when the player presses the Interact button.
    /// Only the closest interactable will respond.
    /// </summary>
    private void OnInteract(Interact e)
    {
        // If this interact event already has a questGiver, it's a secondary event - ignore
        if (e.questGiver != null) return;
        
        if (currentClosestInteractable != null && currentClosestInteractable.IsInteractable())
        {
            currentClosestInteractable.OnInteract();
        }
    }
    
    /// <summary>
    /// Force an immediate update of the closest interactable.
    /// Call this when interactables are added/removed or state changes.
    /// </summary>
    public void ForceUpdate()
    {
        UpdateClosestInteractable();
    }
    
    public float GetDefaultInteractionDistance()
    {
        return defaultInteractionDistance;
    }
}

/// <summary>
/// Interface for objects that can be interacted with via proximity.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Get the world position of this interactable.
    /// </summary>
    Vector3 GetPosition();
    
    /// <summary>
    /// Get the maximum distance from which this can be interacted with.
    /// </summary>
    float GetInteractionDistance();
    
    /// <summary>
    /// Whether this interactable can currently be interacted with.
    /// </summary>
    bool IsInteractable();
    
    /// <summary>
    /// Called when the player interacts with this object.
    /// </summary>
    void OnInteract();
    
    /// <summary>
    /// Show or hide the interact bubble for this object.
    /// </summary>
    void SetInteractBubbleActive(bool active);
}
