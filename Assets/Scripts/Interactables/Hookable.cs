using UnityEngine;

public class Hookable : Interactable
{
    private Animator animator;
    private Renderer renderer;
    private Color originalColor; // Store the original color of the chest

    [SerializeField]
    private Color highlightColor = Color.green;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        renderer = GetComponent<Renderer>();

        if (animator == null)
        {
            Debug.LogError("Animator component missing on " + gameObject.name);
        }

        if (renderer == null)
        {
            Debug.LogError("Renderer component missing on " + gameObject.name);
        }
        else
        {
            // Store the original color of the material
            originalColor = renderer.material.color;
        }
    }

    public override void Interact()
    {
        if (!isInteractable)
        {
            Debug.Log("Can't interact with " + gameObject.name);
            return;
        }
    }

    protected override void Highlight()
    {
        base.Highlight();

        // Change the object's color to the highlight color when highlighted
        if (renderer != null)
        {
            renderer.material.color = highlightColor;
        }
    }

    protected override void Unhighlight()
    {
        base.Unhighlight();

        // Revert the object's color to the original color when unhighlighted
        if (renderer != null)
        {
            renderer.material.color = originalColor;
        }
    }
}
