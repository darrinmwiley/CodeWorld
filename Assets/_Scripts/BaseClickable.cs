using UnityEngine;

// This ensures all the "worker" scripts are present on the GameObject
[RequireComponent(typeof(LookListener))]
[RequireComponent(typeof(ClickListener))]
[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(OutlineOnLook))]
[RequireComponent(typeof(MeshCollider))] // Ensures a MeshCollider exists
public class BaseClickable : MonoBehaviour
{
    protected LookListener lookListener;
    protected ClickListener clickListener;
    protected OutlineOnLook outlineOnLook;
    protected Outline outline;
    protected MeshCollider meshCollider;

    protected virtual void Awake()
    {
        // 1. Get references
        lookListener = GetComponent<LookListener>();
        clickListener = GetComponent<ClickListener>();
        outlineOnLook = GetComponent<OutlineOnLook>();
        outline = GetComponent<Outline>();
        meshCollider = GetComponent<MeshCollider>();

        // 2. Set MeshCollider to Convex by default
        meshCollider.convex = true;

        // 3. Ensure outline starts disabled so it doesn't glow until you look at it
        outline.enabled = false;

        // 4. Wire OutlineOnLook to the listeners and the outline component
        outlineOnLook.lookListener = lookListener;
        outlineOnLook.outline = outline;

        // 5. Wire ClickListener to the LookListener
        clickListener.lookListener = lookListener;

        // 6. Register the default click handler
        clickListener.AddClickHandler(HandleClick);
    }

    protected virtual void HandleClick()
    {
    }
}