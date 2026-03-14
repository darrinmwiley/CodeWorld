using UnityEngine;

// This ensures all the "worker" scripts are present on the GameObject
[RequireComponent(typeof(LookListener))]
[RequireComponent(typeof(ClickListener))]
[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(OutlineOnLook))]
public class BaseClickable : MonoBehaviour
{
    protected LookListener lookListener;
    protected ClickListener clickListener;
    protected OutlineOnLook outlineOnLook;
    protected Outline outline;

    protected virtual void Awake()
    {
        // Grab references
        lookListener = GetComponent<LookListener>();
        clickListener = GetComponent<ClickListener>();
        outlineOnLook = GetComponent<OutlineOnLook>();
        outline = GetComponent<Outline>();
        outline.enabled = false;

        // Link OutlineOnLook to the other components so it knows what to toggle
        outlineOnLook.lookListener = lookListener;
        outlineOnLook.outline = outline;

        // Link ClickListener to LookListener so clicks only work while looking
        clickListener.lookListener = lookListener;

        // Register our click handler
        clickListener.AddClickHandler(HandleClick);
    }

    protected virtual void HandleClick()
    {
        Debug.Log($"{gameObject.name} clicked!");
    }
}