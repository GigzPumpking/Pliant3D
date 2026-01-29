using UnityEngine;

public class FetchableInteractable : Interactable
{
    private Animator animator;
    private Renderer[] renderers; // Array to store all renderers in this object and its children
    private Color[] originalColors; // Array to store the original colors of each renderer

    [SerializeField]
    private Color highlightColor = Color.green;

    public bool isFetched = false;

    private bool inRadius = false;

    void OnEnable() {
        EventDispatcher.AddListener<Interact>(PlayerInteract);
        
    }

    void OnDisable() {
        EventDispatcher.RemoveListener<Interact>(PlayerInteract);
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            inRadius = true;
            Highlight();
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            inRadius = false;
            Unhighlight();
        }
    }

    void PlayerInteract(Interact e) {
        if (inRadius && !isFetched) {
            Interact();
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<Renderer>();

        if (animator == null)
        {
            Debug.LogWarning("Animator component missing on " + gameObject.name);
        }

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No Renderer components found on " + gameObject.name + " or its children.");
        }
        else
        {
            // Store the original colors of all renderers
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }

    public override void Interact()
    {
        if (!isInteractable)
        {
            Debug.Log("Can't interact with " + gameObject.name);
            return;
        }

        isFetched = true;

        // Hide the object visually

        gameObject.SetActive(false);
        
        EventDispatcher.Raise<FetchObjectInteract>(new FetchObjectInteract() { fetchableObject = this });
    }

    protected override void Highlight()
    {
        base.Highlight();

        // Change the color of all renderers to the highlight color
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = highlightColor;
            }
        }
    }

    protected override void Unhighlight()
    {
        base.Unhighlight();

        // Revert the color of all renderers to their original colors
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }
}
